import NetInfo from '@react-native-community/netinfo';
import { env } from '../config/env';
import { secureStorage } from '../auth/secureStorage';
import { mapErrorToUserMessage } from '../utils/errorMapper';
import type { EntrarResponse } from './types';

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    /** Sprint 17 (ADR 0020) — texto original do backend/exceção, nunca mostrado ao morador. Só para log/diagnóstico técnico. */
    public technicalMessage?: string,
  ) {
    super(message);
  }
}

/**
 * AuthContext se registra aqui para saber quando a sessão expirou de verdade (refresh
 * token também inválido) — sem isso, a sessão local é limpa por tryRefresh() mas o
 * estado `user` em memória continua populado, deixando o app "logado" numa tela
 * protegida enquanto toda chamada subsequente falha silenciosamente com 401.
 */
let onSessionExpired: (() => void) | null = null;

export function registerSessionExpiredHandler(handler: () => void): void {
  onSessionExpired = handler;
}

/**
 * Sprint 17 (ADR 0020) — todo erro passa por `mapErrorToUserMessage` antes de virar
 * `ApiError.message`. Isso significa que toda tela já existente (`err instanceof
 * ApiError ? err.message : fallback`) fica protegida automaticamente, sem precisar
 * editar cada uma — a mensagem já chega amigável na origem.
 */
async function construirErroDeApi(status: number | undefined, mensagemTecnica: string): Promise<ApiError> {
  const rede = await NetInfo.fetch();
  const temConexaoInternet = rede.isConnected !== false;

  if (__DEV__) {
    console.warn('[API]', status, mensagemTecnica);
  }

  const amigavel = mapErrorToUserMessage({ status, mensagemTecnica, temConexaoInternet });
  return new ApiError(status ?? 0, amigavel.mensagem, mensagemTecnica);
}

/**
 * Sprint 18.1 (hotfix) — sem isso, um backend inalcançável (comum num celular
 * físico numa rede diferente da máquina de desenvolvimento) deixava `fetch`
 * pendurado indefinidamente: a tela de propriedades ficava presa em
 * `loading=true` para sempre ("não carrega") e o logout nunca terminava de
 * chamar `setUser(null)` porque o `await` do POST de logout nunca resolvia
 * ("não sai da conta"). 15s é generoso o bastante para uma rede móvel lenta
 * sem deixar o usuário esperando indefinidamente por um servidor que não vai
 * responder.
 */
const TIMEOUT_REQUISICAO_MS = 15000;

async function request<T>(path: string, options: RequestInit = {}, allowRefresh = true): Promise<T> {
  const accessToken = await secureStorage.getAccessToken();

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), TIMEOUT_REQUISICAO_MS);

  let response: Response;
  try {
    response = await fetch(`${env.apiUrl}${path}`, {
      ...options,
      signal: controller.signal,
      headers: {
        'Content-Type': 'application/json',
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
        ...options.headers,
      },
    });
  } catch (err) {
    const mensagem = err instanceof Error && err.name === 'AbortError' ? 'tempo esgotado' : err instanceof Error ? err.message : String(err);
    // Falha antes de qualquer resposta existir (sem internet, servidor inalcançável, timeout).
    throw await construirErroDeApi(undefined, mensagem);
  } finally {
    clearTimeout(timeoutId);
  }

  if (response.status === 401 && allowRefresh && accessToken) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      return request<T>(path, options, false);
    }
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const mensagemTecnica = body?.error ?? `HTTP ${response.status}`;
    throw await construirErroDeApi(response.status, mensagemTecnica);
  }

  return body as T;
}

/** Uma tentativa só de renovar via refresh token — se falhar, limpa a sessão local. */
async function tryRefresh(): Promise<boolean> {
  const refreshToken = await secureStorage.getRefreshToken();
  if (!refreshToken) {
    return false;
  }

  const response = await fetch(`${env.apiUrl}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  });

  if (!response.ok) {
    await secureStorage.clear();
    onSessionExpired?.();
    return false;
  }

  const data = (await response.json()) as EntrarResponse;
  await secureStorage.setTokens(data.accessToken, data.refreshToken);
  return true;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: 'GET' }),

  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),

  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body !== undefined ? JSON.stringify(body) : undefined }),

  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
};
