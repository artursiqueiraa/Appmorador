import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';

const DISPOSITIVO_PUSH_ID_KEY = 'dispositivoPushId';
const PREFERENCIAS_LOCAIS_KEY = 'dispositivoPushPreferenciasLocais';

export interface PreferenciasNotificacaoLocais {
  notificarAlertas: boolean;
  notificarAtividades: boolean;
  notificarGeral: boolean;
}

const PREFERENCIAS_PADRAO: PreferenciasNotificacaoLocais = {
  notificarAlertas: true,
  notificarAtividades: true,
  notificarGeral: true,
};

/**
 * Sprint 19 — mesmo padrão de fallback web do `secureStorage.ts` (SecureStore não
 * tem implementação nativa em navegador), duplicado aqui deliberadamente: são só
 * 2 chaves, e criar uma dependência cruzada com o storage de sessão/autenticação
 * misturaria dois ciclos de vida diferentes (o id do dispositivo sobrevive ao
 * logout — ver `desregistrarAsync`).
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

export const obterDispositivoPushId = (): Promise<string | null> => storage.getItemAsync(DISPOSITIVO_PUSH_ID_KEY);

export const salvarDispositivoPushId = (id: string): Promise<void> => storage.setItemAsync(DISPOSITIVO_PUSH_ID_KEY, id);

export const limparDispositivoPushId = (): Promise<void> => storage.deleteItemAsync(DISPOSITIVO_PUSH_ID_KEY);

export async function obterPreferenciasLocais(): Promise<PreferenciasNotificacaoLocais> {
  const raw = await storage.getItemAsync(PREFERENCIAS_LOCAIS_KEY);
  return raw ? (JSON.parse(raw) as PreferenciasNotificacaoLocais) : PREFERENCIAS_PADRAO;
}

export const salvarPreferenciasLocais = (preferencias: PreferenciasNotificacaoLocais): Promise<void> =>
  storage.setItemAsync(PREFERENCIAS_LOCAIS_KEY, JSON.stringify(preferencias));
