import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '../stores/authStore';

const API_URL = import.meta.env.VITE_API_URL;

if (!API_URL) {
  throw new Error('VITE_API_URL não configurada. Defina no arquivo .env (ou .env.local) na raiz do Painel Web.');
}

export const httpClient = axios.create({ baseURL: API_URL });

/** Sprint 22A — injeta o Bearer token atual (já é o token de impersonation quando aplicável, ver authStore). */
httpClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const { accessToken } = useAuthStore.getState();
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`);
  }
  return config;
});

let onSessionExpired: (() => void) | null = null;

export function registerSessionExpiredHandler(handler: (() => void) | null): void {
  onSessionExpired = handler;
}

let refreshEmAndamento: Promise<string | null> | null = null;

async function tentarRefresh(): Promise<string | null> {
  const { refreshToken, user, impersonation, setSession } = useAuthStore.getState();

  // Sprint 22A (ADR 0021) — impersonation nunca tem refresh token (token de 15min, sem
  // renovação, por design). Uma sessão de impersonation expirada some sozinha, não tenta refresh.
  if (impersonation || !refreshToken || !user) {
    return null;
  }

  if (!refreshEmAndamento) {
    refreshEmAndamento = axios
      .post<{ accessToken: string; refreshToken: string; usuarioId: string; nome: string; email: string }>(
        `${API_URL}/api/auth/refresh`,
        { refreshToken },
      )
      .then((resposta) => {
        setSession(resposta.data.accessToken, resposta.data.refreshToken, {
          id: resposta.data.usuarioId,
          nome: resposta.data.nome,
          email: resposta.data.email,
        });
        return resposta.data.accessToken;
      })
      .catch(() => null)
      .finally(() => {
        refreshEmAndamento = null;
      });
  }

  return refreshEmAndamento;
}

httpClient.interceptors.response.use(
  (resposta) => resposta,
  async (erro: AxiosError) => {
    const requisicaoOriginal = erro.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;

    if (erro.response?.status === 401 && requisicaoOriginal && !requisicaoOriginal._retry) {
      requisicaoOriginal._retry = true;
      const novoToken = await tentarRefresh();

      if (novoToken) {
        requisicaoOriginal.headers.set('Authorization', `Bearer ${novoToken}`);
        return httpClient(requisicaoOriginal);
      }

      useAuthStore.getState().clearSession();
      onSessionExpired?.();
    }

    return Promise.reject(erro);
  },
);

export function extrairMensagemErro(erro: unknown, mensagemPadrao = 'Algo deu errado. Tente novamente.'): string {
  if (axios.isAxiosError(erro)) {
    const corpo = erro.response?.data as { error?: string } | undefined;
    if (corpo?.error) return corpo.error;
    if (erro.code === 'ECONNABORTED' || !erro.response) return 'Não foi possível conectar ao servidor.';
  }
  return mensagemPadrao;
}
