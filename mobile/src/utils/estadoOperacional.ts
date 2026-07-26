import { colors } from '../theme/theme';
import type { EstadoOperacional } from '../api/types';

/** Sprint 13 — Camada Operacional Unificada. Única fonte de rótulo/cor para os 4 estados — nunca reimplementado em cada tela. */
export function rotuloEstadoOperacional(estado: EstadoOperacional): string {
  switch (estado) {
    case 'Saudavel':
      return 'Saudável';
    case 'Atencao':
      return 'Atenção';
    case 'Critico':
      return 'Crítico';
    case 'Offline':
      return 'Offline';
  }
}

export function corEstadoOperacional(estado: EstadoOperacional): string {
  switch (estado) {
    case 'Saudavel':
      return colors.safe;
    case 'Atencao':
      return colors.warn;
    case 'Critico':
      return colors.danger;
    case 'Offline':
      return colors.mute;
  }
}

export function emojiEstadoOperacional(estado: EstadoOperacional): string {
  switch (estado) {
    case 'Saudavel':
      return '🟢';
    case 'Atencao':
      return '🟡';
    case 'Critico':
      return '🔴';
    case 'Offline':
      return '⚫';
  }
}
