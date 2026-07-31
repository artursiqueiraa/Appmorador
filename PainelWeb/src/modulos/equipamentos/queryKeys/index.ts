import type { FiltroEquipamentos } from '../types';

/** Sprint 22B (ADR 0031) — fábrica única de query keys do módulo Equipamentos. */
export const equipamentosKeys = {
  all: ['equipamentos'] as const,
  lista: (pagina: number, tamanhoPagina: number, filtro: FiltroEquipamentos) =>
    [...equipamentosKeys.all, 'lista', pagina, tamanhoPagina, filtro] as const,
  detalhe: (id: string) => [...equipamentosKeys.all, 'detalhe', id] as const,
};
