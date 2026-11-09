import { Platform } from 'react-native';
import { useSyncExternalStore } from 'react';

/**
 * Sprint 25 (ADR 0041) — código de convite que chegou pelo link (https://…/convite/{código}).
 * Fica guardado até a pessoa entrar/criar conta e aceitar (ou dispensar). No navegador também vai
 * para o localStorage, para sobreviver ao cadastro; falhas de armazenamento são ignoradas.
 */
const CHAVE = 'akteck.convitePendente';
let codigo: string | null = null;
const ouvintes = new Set<() => void>();

function emitir() {
  ouvintes.forEach((o) => o());
}

function lerArmazenado(): string | null {
  try {
    return Platform.OS === 'web' ? globalThis.localStorage?.getItem(CHAVE) ?? null : null;
  } catch {
    return null;
  }
}

function gravar(valor: string | null) {
  try {
    if (Platform.OS === 'web') {
      if (valor) {
        globalThis.localStorage?.setItem(CHAVE, valor);
      } else {
        globalThis.localStorage?.removeItem(CHAVE);
      }
    }
  } catch {
    // armazenamento bloqueado: segue só em memória
  }
}

export const conviteStore = {
  obter: () => codigo,
  definir(valor: string | null) {
    codigo = valor;
    gravar(valor);
    emitir();
  },
  assinar(o: () => void) {
    ouvintes.add(o);
    return () => {
      ouvintes.delete(o);
    };
  },
};

/** Lê /convite/{código} da URL (navegador) ou o que ficou guardado; limpa a URL. */
export function capturarConviteDaUrl(): void {
  if (Platform.OS !== 'web') {
    return;
  }
  try {
    const caminho = globalThis.location?.pathname ?? '';
    const m = /\/convite\/([A-Za-z0-9_-]{10,})/.exec(caminho);
    if (m) {
      conviteStore.definir(m[1]);
      globalThis.history?.replaceState(null, '', '/');
      return;
    }
  } catch {
    // sem URL acessível
  }
  codigo = lerArmazenado();
}

export function useConvitePendente(): string | null {
  return useSyncExternalStore(conviteStore.assinar, conviteStore.obter, conviteStore.obter);
}
