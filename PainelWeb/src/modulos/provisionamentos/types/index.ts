import type { PaginadoResponse } from '../../../compartilhado/tipos/paginacao';

/** Espelha `VinculoResponse` (backend, `AppMorador.Application.Painel.VinculosEquipamento`). */
export interface Vinculo {
  id: string;
  equipamentoId: string;
  equipamentoNome?: string | null;
  propriedadeId: string;
  propriedadeNome?: string | null;
  dataInicioUtc: string;
  dataFimUtc?: string | null;
  ativo: boolean;
  criadoPorUsuarioId: string;
  observacoes?: string | null;
}

export type VinculosPaginados = PaginadoResponse<Vinculo>;

export interface DashboardAlocacao {
  totalEquipamentos: number;
  totalProvisionados: number;
  totalDisponiveis: number;
}

export interface ProvisionarRequest {
  equipamentoId: string;
  propriedadeId: string;
  observacoes?: string;
}

export interface TrocarRequest {
  propriedadeId: string;
  equipamentoAntigoId: string;
  equipamentoNovoId: string;
  observacoes?: string;
}
