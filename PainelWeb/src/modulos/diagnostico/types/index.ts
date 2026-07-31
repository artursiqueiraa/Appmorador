import type { PaginadoResponse } from '../../../compartilhado/tipos/paginacao';
import type {
  EstadoOperacionalEquipamento,
  FabricanteEquipamento,
  StatusEquipamento,
} from '../../../compartilhado/tipos/equipamento';

/** Espelha `DiagnosticoEquipamentoResponse` (backend, `AppMorador.Application.Painel.Diagnostico`). */
export interface DiagnosticoEquipamento {
  equipamentoId: string;
  equipamentoNome: string;
  fabricante: FabricanteEquipamento;
  propriedadeId: string;
  propriedadeNome: string;
  status: StatusEquipamento;
  estadoOperacional: EstadoOperacionalEquipamento;
  ultimoPingUtc?: string | null;
  temProblemaAtivo?: boolean | null;
  quantidadeEventosRecentes: number;
  ultimoEventoDescricao?: string | null;
  ultimoEventoEmUtc?: string | null;
}

export type DiagnosticoEquipamentosPaginados = PaginadoResponse<DiagnosticoEquipamento>;
