import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';

export type Perfil = 'morador' | 'tecnico';

const CHAVE = 'perfilPreferencia';
const PADRAO: Perfil = 'morador';

/**
 * Sprint 17 (ADR 0020) — o domínio não tem RBAC (dívida técnica item 6): não existe
 * campo de perfil em nenhuma resposta de API. Esta preferência é 100% local
 * (`expo-secure-store`, mesmo padrão de `onboardingStorage.ts`) e serve só para
 * organizar a UI (esconder telas técnicas do morador comum por padrão) — **nunca é
 * uma fronteira de segurança real**. Qualquer pessoa com acesso ao próprio celular
 * pode alternar para "técnico" livremente; nenhum dado sensível depende disso para
 * proteção (o backend continua validando posse/autenticação normalmente).
 */
const webFallbackStorage = {
  getItemAsync: async (key: string) => (typeof window === 'undefined' ? null : window.localStorage.getItem(key)),
  setItemAsync: async (key: string, value: string) => {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(key, value);
    }
  },
};

const storage = Platform.OS === 'web' ? webFallbackStorage : SecureStore;

export async function obterPerfil(): Promise<Perfil> {
  const valor = await storage.getItemAsync(CHAVE);
  return valor === 'tecnico' ? 'tecnico' : PADRAO;
}

export async function salvarPerfil(perfil: Perfil): Promise<void> {
  await storage.setItemAsync(CHAVE, perfil);
}
