import { httpClient } from './httpClient';
import type { PropriedadeResponse } from '../types/api';

export interface CameraResumo {
  id: string;
  nome: string;
  status: 'Desconhecido' | 'Online' | 'Offline';
  ultimaImagemUrl?: string | null;
  ultimaVezVistaUtc?: string | null;
}

export interface SnapshotOperacional {
  geradoEmUtc: string;
  saude: 'Saudavel' | 'Atencao' | 'Critico' | 'Offline';
  quantidadeEquipamentosOnline: number;
  quantidadeEquipamentosOffline: number;
  ultimaComunicacaoUtc?: string | null;
  quantidadeEventosHoje: number;
  quantidadeAlarmesAtivos: number;
  quantidadeFalhasDetectadas: number;
}

export interface EventoTimeline {
  id: string;
  titulo: string;
  descricao?: string | null;
  ocorridoEmUtc: string;
  destaque: boolean;
}

/**
 * Sprint 22A (Fase 6) — Diagnóstico da Propriedade reaproveita 100% dos endpoints já
 * ownership-checked do cliente (GET /api/properties, /cameras, /operacional/snapshot,
 * /eventos) — durante impersonation, o token JÁ age como o cliente-alvo (ver ADR 0021), então
 * nenhum endpoint novo foi necessário para esta tela.
 */
export const propriedadesService = {
  listarMinhas: () => httpClient.get<PropriedadeResponse[]>('/api/properties').then((r) => r.data),

  listarCameras: (propriedadeId: string) =>
    httpClient.get<CameraResumo[]>(`/api/properties/${propriedadeId}/cameras`).then((r) => r.data),

  obterSnapshotOperacional: (propriedadeId: string) =>
    httpClient.get<SnapshotOperacional>(`/api/properties/${propriedadeId}/operacional/snapshot`).then((r) => r.data),

  listarUltimosEventos: (propriedadeId: string) =>
    httpClient
      .get<{ itens: EventoTimeline[] }>(`/api/properties/${propriedadeId}/eventos`, { params: { tamanhoPagina: 5 } })
      .then((r) => r.data.itens),
};
