/**
 * Sprint 16 (ADR 0019, UX001) — raios de borda oficiais. `radius` (flat, valores
 * originais da Sprint 2: md=11/lg=14/xl=16/xxl=22) continua existindo porque todas
 * as telas já existentes o usam assim — divergem em 1-2dp do `borderRadius` oficial,
 * dentro da tolerância de fidelidade desta Sprint (ver ADR 0019); nunca removido.
 */
export type BorderRadius = {
  sm: number;
  md: number;
  lg: number;
  xl: number;
  full: number;
};

export const borderRadius: BorderRadius = {
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  full: 999,
};

/** Nomes/valores de conveniência já usados por todas as telas existentes desde a Sprint 2. */
export const radius = {
  sm: 8,
  md: 11,
  lg: 14,
  xl: 16,
  xxl: 22,
  pill: 999,
};
