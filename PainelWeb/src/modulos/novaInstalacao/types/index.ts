import type { TipoPropriedade } from '../../../compartilhado/tipos/propriedade';
import type { FabricanteEquipamento } from '../../../compartilhado/tipos/equipamento';

/** Espelha `CriarNovaInstalacaoRequest` (backend, `AppMorador.Application.Painel.NovasInstalacoes`). */
export interface CriarNovaInstalacaoRequest {
  nomeCliente: string;
  emailCliente: string;
  senhaInicial: string;
  nomePropriedade: string;
  tipoPropriedade: TipoPropriedade;
  enderecoPropriedade?: string;
  equipamentoId?: string;
}

/** Espelha `NovaInstalacaoResponse`. */
export interface NovaInstalacaoResponse {
  clienteId: string;
  propriedadeId: string;
  nomePropriedade: string;
  vinculoEquipamentoId?: string | null;
}

export interface EquipamentoLivre {
  id: string;
  nome: string;
  fabricante: FabricanteEquipamento;
  numeroSerie?: string | null;
}
