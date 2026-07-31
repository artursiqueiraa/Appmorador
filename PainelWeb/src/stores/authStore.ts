import { create } from 'zustand';
import { jwtDecode } from 'jwt-decode';
import type { DecodedToken, ImpersonationState, StoredUser } from '../types/auth';

const CHAVE_ACCESS_TOKEN = 'painel.accessToken';
const CHAVE_REFRESH_TOKEN = 'painel.refreshToken';
const CHAVE_USER = 'painel.user';
const CHAVE_IMPERSONATION = 'painel.impersonation';
const CHAVE_RETURN_URL = 'painel.returnUrl';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: StoredUser | null;
  impersonation: ImpersonationState | null;
  isLoading: boolean;

  setSession: (accessToken: string, refreshToken: string, user: StoredUser) => void;
  clearSession: () => void;
  startImpersonation: (impersonationToken: string, info: ImpersonationState) => void;
  endImpersonation: () => string | null;
  setLoading: (loading: boolean) => void;
  getDecoded: () => DecodedToken | null;
}

function lerStorage<T>(chave: string): T | null {
  const bruto = localStorage.getItem(chave);
  if (!bruto) return null;
  try {
    return JSON.parse(bruto) as T;
  } catch {
    return null;
  }
}

/**
 * Sprint 22A — Zustand store de autenticação, persistida em localStorage (mesmo padrão de
 * `AuthContext.tsx` do app mobile, adaptado para web). `Nome` nunca vem do JWT (ver
 * `types/auth.ts`) — persistido separadamente a partir do corpo de `EntrarResponse`.
 *
 * Impersonation: em vez de um header `X-Impersonar-Propriedade-Id` (a missão original supunha
 * isso), o token de impersonation JÁ contém toda a identidade necessária (claims do usuário
 * ALVO, ver ADR 0021) — o interceptor do Axios simplesmente troca qual token é usado como Bearer
 * enquanto `impersonation` não é nulo, sem precisar de nenhum header extra.
 */
export const useAuthStore = create<AuthState>((set, get) => ({
  accessToken: lerStorage<string>(CHAVE_ACCESS_TOKEN),
  refreshToken: lerStorage<string>(CHAVE_REFRESH_TOKEN),
  user: lerStorage<StoredUser>(CHAVE_USER),
  impersonation: lerStorage<ImpersonationState>(CHAVE_IMPERSONATION),
  isLoading: false,

  setSession: (accessToken, refreshToken, user) => {
    localStorage.setItem(CHAVE_ACCESS_TOKEN, JSON.stringify(accessToken));
    localStorage.setItem(CHAVE_REFRESH_TOKEN, JSON.stringify(refreshToken));
    localStorage.setItem(CHAVE_USER, JSON.stringify(user));
    set({ accessToken, refreshToken, user });
  },

  clearSession: () => {
    localStorage.removeItem(CHAVE_ACCESS_TOKEN);
    localStorage.removeItem(CHAVE_REFRESH_TOKEN);
    localStorage.removeItem(CHAVE_USER);
    localStorage.removeItem(CHAVE_IMPERSONATION);
    set({ accessToken: null, refreshToken: null, user: null, impersonation: null });
  },

  startImpersonation: (impersonationToken, info) => {
    localStorage.setItem(CHAVE_IMPERSONATION, JSON.stringify(info));
    set({ accessToken: impersonationToken, impersonation: info });
  },

  endImpersonation: () => {
    const { impersonation } = get();
    localStorage.removeItem(CHAVE_IMPERSONATION);
    set({ accessToken: impersonation?.tokenOriginal ?? null, impersonation: null });
    return impersonation?.propriedadeId ?? null;
  },

  setLoading: (loading) => set({ isLoading: loading }),

  getDecoded: () => {
    const { accessToken } = get();
    if (!accessToken) return null;
    try {
      return jwtDecode<DecodedToken>(accessToken);
    } catch {
      return null;
    }
  },
}));

export function salvarUrlDeRetorno(url: string): void {
  sessionStorage.setItem(CHAVE_RETURN_URL, url);
}

export function consumirUrlDeRetorno(): string | null {
  const url = sessionStorage.getItem(CHAVE_RETURN_URL);
  sessionStorage.removeItem(CHAVE_RETURN_URL);
  return url;
}
