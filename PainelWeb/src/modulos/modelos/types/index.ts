import type { FabricanteEquipamento } from '../../../compartilhado/tipos/equipamento';

export type CapacidadeEquipamento = 'Face' | 'Tag' | 'QrCode' | 'Senha' | 'Armar' | 'Desarmar' | 'Pgm' | 'Streaming' | 'Ptz';

/** Espelha `ModeloEquipamentoResponse` (backend, `AppMorador.Application.Equipamentos`). */
export interface ModeloEquipamento {
  id: string;
  fabricante: FabricanteEquipamento;
  nome: string;
  capacidades: CapacidadeEquipamento[];
}
