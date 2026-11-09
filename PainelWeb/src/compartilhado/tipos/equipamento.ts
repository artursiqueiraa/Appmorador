/**
 * Sprint 22B (ADR 0031) — vocabulário compartilhado entre os módulos Equipamentos e Diagnóstico
 * (ambos descrevem o mesmo `Equipamento` do backend). Vive em `compartilhado` em vez de um módulo
 * importar o `types` do outro, respeitando a Regra de Ouro de isolamento entre módulos.
 */
export type FabricanteEquipamento = 'ControlId' | 'Jfl' | 'Intelbras';

export type StatusEquipamento = 'Desconhecido' | 'Online' | 'Offline';

export type EstadoOperacionalEquipamento = 'Ativo' | 'EmManutencao' | 'Inativo' | 'Defeituoso';

/** Akteck Seg (ADR 0035) — nome comercial do fabricante (nunca o valor do enum, "ControlId"/"Jfl"). */
export const ROTULO_FABRICANTE: Record<FabricanteEquipamento, string> = {
  ControlId: 'Control iD',
  Jfl: 'JFL',
  Intelbras: 'Intelbras',
};
