import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';

export interface OnboardingProgress {
  etapa: number;
  concluido: boolean;
}

const PREFIXO_CHAVE = 'onboarding_';
const ETAPA_INICIAL: OnboardingProgress = { etapa: 0, concluido: false };

/**
 * Sprint 16 (ADR 0019) — persistência do progresso do Onboarding, por Propriedade
 * (não por usuário — cada propriedade tem sua própria configuração). Corrige o bug
 * "onboarding desaparece": antes não existia nenhum registro do progresso, então
 * fechar o app no meio da configuração perdia tudo, sem nenhum jeito de retomar.
 * Reaproveita `expo-secure-store` (já uma dependência) em vez de adicionar uma nova
 * biblioteca só para isso.
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

export async function obterProgresso(propriedadeId: string): Promise<OnboardingProgress> {
  const bruto = await storage.getItemAsync(`${PREFIXO_CHAVE}${propriedadeId}`);
  if (!bruto) {
    return ETAPA_INICIAL;
  }

  try {
    return JSON.parse(bruto) as OnboardingProgress;
  } catch {
    return ETAPA_INICIAL;
  }
}

export async function salvarProgresso(propriedadeId: string, progresso: OnboardingProgress): Promise<void> {
  await storage.setItemAsync(`${PREFIXO_CHAVE}${propriedadeId}`, JSON.stringify(progresso));
}

/** Propriedades criadas antes desta Sprint nunca tiveram um Wizard — tratadas como já configuradas, nunca forçadas a passar por ele retroativamente. */
export async function marcarComoConcluidoRetroativamente(propriedadeId: string): Promise<void> {
  await salvarProgresso(propriedadeId, { etapa: 0, concluido: true });
}
