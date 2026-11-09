import NetInfo from '@react-native-community/netinfo';
import { env } from '../config/env';
import { ehWeb, secureStorage } from '../auth/secureStorage';
import { mapErrorToUserMessage } from '../utils/errorMapper';
import type { EntrarResponse } from './types';

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    /** Sprint 17 (ADR 0020) — texto original do backend/exceção, nunca mostrado ao morador. Só para log/diagnóstico técnico. */
    public technicalMessage?: string,
    /** Sprint 27 (ADR 0043) — código estável do backend (ex.: PIN_INCORRETO), para a tela decidir o que fazer. */
    public codigo?: string,
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

/**
 * Sprint 27 (ADR 0043, dívida 51) — no web, a Api entrega o refresh token num cookie HttpOnly
 * (restrito a /api/auth) quando vê `X-Cliente: web`; `credentials: 'include'` faz o navegador
 * enviá-lo de volta. No celular, nada disso se aplica.
 */
const cabecalhoClienteWeb: Record<string, string> = ehWeb ? { 'X-Cliente': 'web' } : {};
const opcoesSessaoWeb: RequestInit = ehWeb ? { credentials: 'include' } : {};

async function request<T>(path: string, options: RequestInit = {}, allowRefresh = true): Promise<T> {
  const accessToken = await secureStorage.getAccessToken();

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), TIMEOUT_REQUISICAO_MS);

  let response: Response;
  try {
    response = await fetch(`${env.apiUrl}${path}`, {
      ...options,
      ...opcoesSessaoWeb,
      signal: controller.signal,
      headers: {
        // FormData (upload de foto) define o próprio Content-Type com o boundary.
        ...(options.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }),
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
        ...cabecalhoClienteWeb,
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
    const erro = await construirErroDeApi(response.status, mensagemTecnica);
    erro.codigo = typeof body?.codigo === 'string' ? body.codigo : undefined;
    throw erro;
  }

  return body as T;
}

/** Uma tentativa só de renovar via refresh token — se falhar, limpa a sessão local. */
async function tryRefresh(): Promise<boolean> {
  const refreshToken = await secureStorage.getRefreshToken();
  if (!refreshToken && !ehWeb) {
    return false;
  }

  let response: Response;
  try {
    response = await fetch(`${env.apiUrl}/api/auth/refresh`, {
      method: 'POST',
      ...opcoesSessaoWeb,
      headers: { 'Content-Type': 'application/json', ...cabecalhoClienteWeb },
      // No web vai vazio: o token vem do cookie HttpOnly.
      body: JSON.stringify({ refreshToken: refreshToken ?? '' }),
    });
  } catch {
    return false;
  }

  if (!response.ok) {
    await secureStorage.clear();
    onSessionExpired?.();
    return false;
  }

  const data = (await response.json()) as EntrarResponse;
  await secureStorage.setTokens(data.accessToken, data.refreshToken);
  return true;
}

/**
 * Sprint 27 — ao abrir o app web, o access token não existe mais (só vivia em memória): renova a
 * sessão pelo cookie. No celular, só confirma que há token guardado.
 */
export async function restaurarSessao(): Promise<boolean> {
  if (!ehWeb) {
    return (await secureStorage.getAccessToken()) !== null;
  }
  return tryRefresh();
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: 'GET' }),

  /** `cabecalhos`: Sprint 27 — confirmação (PIN) e chave de idempotência dos comandos. */
  post: <T>(path: string, body?: unknown, cabecalhos?: Record<string, string>) =>
    request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined, headers: cabecalhos }),

  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body !== undefined ? JSON.stringify(body) : undefined }),

  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),

  /** Akteck Seg (ADR 0036) — envio multipart (ex.: foto do morador). */
  enviarArquivo: <T>(path: string, formulario: FormData, method: 'PUT' | 'POST' = 'PUT') =>
    request<T>(path, { method, body: formulario }),
};

/**
 * Akteck Seg (ADR 0036) — baixa um binário autenticado (ex.: foto do morador) como Blob. A Api nunca
 * serve essas imagens como arquivo estático, então o navegador não consegue carregá-las por URL
 * direta: o token precisa ir no header.
 */
export async function obterBlobAutenticado(path: string): Promise<Blob | null> {
  const accessToken = await secureStorage.getAccessToken();
  try {
    const resposta = await fetch(`${env.apiUrl}${path}`, {
      headers: { ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}), ...cabecalhoClienteWeb },
    });
    return resposta.ok ? await resposta.blob() : null;
  } catch {
    return null;
  }
}
