import type { PaginadoResponse } from '../../../compartilhado/tipos/paginacao';
import type {
  EstadoOperacionalEquipamento,
  FabricanteEquipamento,
  StatusEquipamento,
} from '../../../compartilhado/tipos/equipamento';

export type { EstadoOperacionalEquipamento, FabricanteEquipamento, StatusEquipamento };

/** Espelha `EquipamentoAdminResponse` (backend, `AppMorador.Application.Painel.Equipamentos`). */
export interface EquipamentoAdmin {
  id: string;
  propriedadeId: string;
  propriedadeNome?: string | null;
  nome: string;
  fabricante: FabricanteEquipamento;
  modelo?: string | null;
  numeroSerie?: string | null;
  status: StatusEquipamento;
  estadoOperacional: EstadoOperacionalEquipamento;
  ip?: string | null;
  porta?: number | null;
  usuario?: string | null;
  macAddress?: string | null;
  observacoes?: string | null;
  createdAtUtc: string;
  excluido: boolean;
  dataExclusaoUtc?: string | null;
  /** Sprint 22C.2 — só as chaves que o Provider do fabricante realmente devolveu (ver `Equipamento.InformacoesDescobertasJson` no backend). Nunca fabricar uma chave que não veio daqui. */
  informacoesDescobertas?: Record<string, string> | null;
  ultimaDescobertaUtc?: string | null;
  ultimaSincronizacaoUtc?: string | null;
}

export type EquipamentosAdminPaginados = PaginadoResponse<EquipamentoAdmin>;

/**
 * Sprint 22C.2 — campos obrigatórios variam por `fabricante` (o formulário dinâmico só envia
 * os relevantes; o backend valida e ignora o resto, ver `EquipamentoAdminServico`):
 * - Jfl: só `numeroSerie`.
 * - ControlId: `ip`/`porta`/`usuario`/`senha`.
 * - Intelbras: `ip`/`porta`/`senha` (sem `usuario`).
 */
export interface CriarEquipamentoRequest {
  propriedadeId: string;
  nome: string;
  fabricante: FabricanteEquipamento;
  modelo?: string;
  numeroSerie?: string;
  estadoOperacional: EstadoOperacionalEquipamento;
  ip?: string;
  porta?: number;
  usuario?: string;
  senha?: string;
  macAddress?: string;
  observacoes?: string;
}

/** Mesma regra condicional de {@link CriarEquipamentoRequest}. `senha` vazia/ausente mantém a senha já cadastrada. */
export interface AtualizarEquipamentoRequest {
  nome: string;
  fabricante: FabricanteEquipamento;
  modelo?: string;
  numeroSerie?: string;
  ip?: string;
  porta?: number;
  usuario?: string;
  senha?: string;
  macAddress?: string;
  observacoes?: string;
}

export interface FiltroEquipamentos {
  busca?: string;
  fabricante?: FabricanteEquipamento | '';
  estadoOperacional?: EstadoOperacionalEquipamento | '';
}
