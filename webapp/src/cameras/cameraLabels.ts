import { formatRelativeTime } from '../utils/formatRelativeTime';
import type { CameraResponse, StatusCamera } from '../api/types';

/**
 * Sprint 20 (ADR 0024) — wording honesto e centralizado: sem monitoramento
 * contínuo, nunca afirmamos "Offline desde X" (um instante que não conhecemos de
 * verdade) — só "última imagem há X", que é sempre verdadeiro tanto para
 * Online quanto Offline.
 */
export function rotuloStatusBadge(status: StatusCamera): string {
  if (status === 'Online') return '🟢 Online';
  if (status === 'Offline') return '🔴 Offline';
  return '⚪ Desconhecido';
}

export function rotuloTimestampCurto(camera: Pick<CameraResponse, 'ultimaVezVistaUtc'>): string {
  return camera.ultimaVezVistaUtc ? formatRelativeTime(camera.ultimaVezVistaUtc) : 'Sem imagem';
}

export function rotuloStatusDetalhado(status: StatusCamera, capturadaEmUtc?: string | null): string {
  const quando = capturadaEmUtc ? formatRelativeTime(capturadaEmUtc) : null;

  if (status === 'Online') {
    return quando ? `Online — última imagem ${quando}` : 'Online';
  }
  if (status === 'Offline') {
    return quando ? `Offline — última imagem ${quando}` : 'Offline — nenhuma imagem disponível ainda';
  }
  return 'Nenhuma imagem disponível ainda';
}
