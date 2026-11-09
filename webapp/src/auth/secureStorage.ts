import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';

const ACCESS_TOKEN_KEY = 'accessToken';
const REFRESH_TOKEN_KEY = 'refreshToken';
const USER_KEY = 'currentUser';

export interface StoredUser {
  id: string;
  nome: string;
  email: string;
}

export const ehWeb = Platform.OS === 'web';

/**
 * Sprint 27 (ADR 0043, dívida 51) — no navegador não existe Keychain/Keystore, e o `localStorage`
 * é legível por qualquer script da página. Por isso, no web:
 * - o refresh token nunca chega ao JavaScript: a Api o entrega num cookie HttpOnly (ver
 *   `AuthController`, cabeçalho `X-Cliente: web`);
 * - o access token fica só em memória (some ao recarregar e é renovado pelo cookie);
 * - no `localStorage` fica apenas o nome/e-mail de quem entrou (não é segredo), para saber que
 *   vale a pena tentar renovar a sessão ao abrir a página.
 * No celular nada muda: tudo no armazenamento seguro do aparelho.
 */
let accessTokenEmMemoria: string | null = null;

const webStorage = {
  getItem: (key: string) => (typeof window === 'undefined' ? null : window.localStorage.getItem(key)),
  setItem: (key: string, value: string) => {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(key, value);
    }
  },
  removeItem: (key: string) => {
    if (typeof window !== 'undefined') {
      window.localStorage.removeItem(key);
    }
  },
};

if (ehWeb) {
  // Versões anteriores guardavam os tokens no localStorage: apaga o que tiver sobrado.
  webStorage.removeItem(ACCESS_TOKEN_KEY);
  webStorage.removeItem(REFRESH_TOKEN_KEY);
}

export const secureStorage = {
  getAccessToken: async (): Promise<string | null> =>
    ehWeb ? accessTokenEmMemoria : SecureStore.getItemAsync(ACCESS_TOKEN_KEY),

  /** No web é sempre null: o refresh token mora no cookie HttpOnly. */
  getRefreshToken: async (): Promise<string | null> => (ehWeb ? null : SecureStore.getItemAsync(REFRESH_TOKEN_KEY)),

  async setTokens(accessToken: string, refreshToken: string): Promise<void> {
    if (ehWeb) {
      accessTokenEmMemoria = accessToken;
      return;
    }
    await SecureStore.setItemAsync(ACCESS_TOKEN_KEY, accessToken);
    await SecureStore.setItemAsync(REFRESH_TOKEN_KEY, refreshToken);
  },

  async getUser(): Promise<StoredUser | null> {
    const raw = ehWeb ? webStorage.getItem(USER_KEY) : await SecureStore.getItemAsync(USER_KEY);
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  },

  async setUser(user: StoredUser): Promise<void> {
    if (ehWeb) {
      webStorage.setItem(USER_KEY, JSON.stringify(user));
      return;
    }
    await SecureStore.setItemAsync(USER_KEY, JSON.stringify(user));
  },

  async clear(): Promise<void> {
    if (ehWeb) {
      accessTokenEmMemoria = null;
      webStorage.removeItem(USER_KEY);
      return;
    }
    await SecureStore.deleteItemAsync(ACCESS_TOKEN_KEY);
    await SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY);
    await SecureStore.deleteItemAsync(USER_KEY);
  },
};
