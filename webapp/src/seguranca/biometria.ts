import * as LocalAuthentication from 'expo-local-authentication';
import * as SecureStore from 'expo-secure-store';

/**
 * Sprint 27 (ADR 0043) — digital/rosto do celular. A biometria nunca vai ao servidor: ela só libera
 * o PIN guardado no armazenamento seguro do aparelho, e o servidor confere o PIN como sempre.
 * Versão web: `biometria.web.ts` (sem biometria no navegador).
 */
const CHAVE_CONFIRMAR = 'seguranca.biometriaConfirmar';
const CHAVE_PIN = 'seguranca.pinBiometria';
const CHAVE_BLOQUEAR = 'seguranca.bloquearApp';

export const biometria = {
  async disponivel(): Promise<boolean> {
    try {
      return (await LocalAuthentication.hasHardwareAsync()) && (await LocalAuthentication.isEnrolledAsync());
    } catch {
      return false;
    }
  },

  /** true quando a pessoa passou pela digital/rosto (ou pela senha da tela do celular). */
  async autenticar(motivo: string): Promise<boolean> {
    try {
      const r = await LocalAuthentication.authenticateAsync({ promptMessage: motivo, cancelLabel: 'Cancelar' });
      return r.success;
    } catch {
      return false;
    }
  },

  async confirmarAtivo(): Promise<boolean> {
    return (await SecureStore.getItemAsync(CHAVE_CONFIRMAR)) === '1' && (await SecureStore.getItemAsync(CHAVE_PIN)) !== null;
  },

  /** Liga a confirmação por biometria guardando o PIN (já validado pela tela) no aparelho. */
  async ativarConfirmacao(pin: string): Promise<void> {
    await SecureStore.setItemAsync(CHAVE_PIN, pin);
    await SecureStore.setItemAsync(CHAVE_CONFIRMAR, '1');
  },

  async desativarConfirmacao(): Promise<void> {
    await SecureStore.deleteItemAsync(CHAVE_PIN);
    await SecureStore.deleteItemAsync(CHAVE_CONFIRMAR);
  },

  /** PIN guardado, liberado pela digital/rosto. null se desligado, cancelado ou falhou. */
  async obterPin(motivo: string): Promise<string | null> {
    if (!(await biometria.confirmarAtivo())) {
      return null;
    }
    if (!(await biometria.autenticar(motivo))) {
      return null;
    }
    return SecureStore.getItemAsync(CHAVE_PIN);
  },

  async bloqueioAtivo(): Promise<boolean> {
    return (await SecureStore.getItemAsync(CHAVE_BLOQUEAR)) === '1';
  },

  async definirBloqueio(ativo: boolean): Promise<void> {
    if (ativo) {
      await SecureStore.setItemAsync(CHAVE_BLOQUEAR, '1');
    } else {
      await SecureStore.deleteItemAsync(CHAVE_BLOQUEAR);
    }
  },
};
