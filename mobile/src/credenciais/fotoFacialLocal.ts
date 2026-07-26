import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';

const PREFIXO_CHAVE = 'fotoFacialLocal_';

/**
 * Sprint 17 (ADR 0020) — não existe endpoint no backend para persistir a foto de um
 * morador (`Morador.FotoPath` não tem rota de escrita, dívida técnica pré-existente).
 * A pré-visualização capturada fica só no dispositivo (URI local do
 * `expo-image-picker`), associada à credencial — se o app for reinstalado ou a foto
 * for limpa do cache do sistema, a miniatura simplesmente desaparece (a credencial em
 * si continua intacta no backend). Nunca finge que a foto está salva de verdade.
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

export async function obterFotoLocal(credencialId: string): Promise<string | null> {
  return storage.getItemAsync(`${PREFIXO_CHAVE}${credencialId}`);
}

export async function salvarFotoLocal(credencialId: string, uri: string): Promise<void> {
  await storage.setItemAsync(`${PREFIXO_CHAVE}${credencialId}`, uri);
}

export async function removerFotoLocal(credencialId: string): Promise<void> {
  await storage.deleteItemAsync(`${PREFIXO_CHAVE}${credencialId}`);
}
