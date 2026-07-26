import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';

export type IconePgm = 'porta' | 'garagem' | 'luz' | 'fechadura' | 'cancela' | 'generico';

export interface RotuloPgm {
  label: string;
  icone: IconePgm;
}

type MapaRotulos = Record<string, RotuloPgm>;

const PREFIXO_CHAVE = 'pgmLabels_';

/**
 * Sprint 17 (ADR 0020) — o backend (`PgmStatusInfo`) só devolve `{numero, acionada,
 * permitida}`, sem nenhum campo de nome amigável — dar um nome como "Abrir portão"
 * a uma PGM exigiria uma coluna nova no backend, fora do escopo desta Sprint. Os
 * rótulos ficam 100% locais (por Equipamento, `expo-secure-store`) — cada
 * instalação/dispositivo tem seus próprios rótulos, coerente com "quem configura é
 * o síndico/técnico daquela propriedade".
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

async function obterMapa(equipamentoId: string): Promise<MapaRotulos> {
  const bruto = await storage.getItemAsync(`${PREFIXO_CHAVE}${equipamentoId}`);
  if (!bruto) {
    return {};
  }

  try {
    return JSON.parse(bruto) as MapaRotulos;
  } catch {
    return {};
  }
}

export async function obterRotulos(equipamentoId: string): Promise<MapaRotulos> {
  return obterMapa(equipamentoId);
}

export async function salvarRotulo(equipamentoId: string, numeroPgm: number, rotulo: RotuloPgm): Promise<void> {
  const mapa = await obterMapa(equipamentoId);
  mapa[numeroPgm] = rotulo;
  await storage.setItemAsync(`${PREFIXO_CHAVE}${equipamentoId}`, JSON.stringify(mapa));
}

export function rotuloPadrao(numeroPgm: number): RotuloPgm {
  return { label: `Comando ${numeroPgm}`, icone: 'generico' };
}
