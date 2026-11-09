/** Sprint 16 (ADR 0019, UX001) — escala de espaçamento oficial. Nenhum componente pode usar um número solto de padding/margin/gap. */
export type Spacing = {
  xs: number;
  sm: number;
  md: number;
  lg: number;
  xl: number;
  xxl: number;
  xxxl: number;
};

export const spacing: Spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  xxxl: 32,
};
