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

/**
 * expo-secure-store não tem implementação nativa para web (não existe Keychain/
 * Keystore em navegador). O app é mobile-first — produção é sempre iOS/Android,
 * onde SecureStore é usado de verdade. Este fallback com localStorage existe só
 * para permitir rodar/testar a versão web durante o desenvolvimento local; nunca
 * é o caminho usado em produção.
 */
const webFallbackStorage = {
  getItemAsync: async (key: string) => (typeof window === 'undefined' ? null : window.localStorage.getItem(key)),
  setItemAsync: async (key: string, value: string) => {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(key, value);
    }
  },
  deleteItemAsync: async (key: string) => {
    if (typeof window !== 'undefined') {
      window.localStorage.removeItem(key);
    }
  },
};

const storage = Platform.OS === 'web' ? webFallbackStorage : SecureStore;

export const secureStorage = {
  getAccessToken: () => storage.getItemAsync(ACCESS_TOKEN_KEY),

  getRefreshToken: () => storage.getItemAsync(REFRESH_TOKEN_KEY),

  async setTokens(accessToken: string, refreshToken: string): Promise<void> {
    await storage.setItemAsync(ACCESS_TOKEN_KEY, accessToken);
    await storage.setItemAsync(REFRESH_TOKEN_KEY, refreshToken);
  },

  async getUser(): Promise<StoredUser | null> {
    const raw = await storage.getItemAsync(USER_KEY);
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  },

  setUser: (user: StoredUser) => storage.setItemAsync(USER_KEY, JSON.stringify(user)),

  async clear(): Promise<void> {
    await storage.deleteItemAsync(ACCESS_TOKEN_KEY);
    await storage.deleteItemAsync(REFRESH_TOKEN_KEY);
    await storage.deleteItemAsync(USER_KEY);
  },
};
