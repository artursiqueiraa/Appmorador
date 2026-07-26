/**
 * Sprint 16 (ADR 0019, UX001) — escala tipográfica oficial. Cada estilo já traz
 * tamanho/peso/altura de linha juntos — nenhum componente novo deve montar esses 3
 * valores separadamente. `fontSize`/`fontWeight` (flat) continuam existindo porque
 * todas as telas já existentes desde a Sprint 2 os usam assim — nunca removidos.
 */
type EstiloTexto = { size: number; weight: string; lineHeight: number };

export type Typography = {
  hero: EstiloTexto;
  h1: EstiloTexto;
  h2: EstiloTexto;
  h3: EstiloTexto;
  body: EstiloTexto;
  caption: EstiloTexto;
  label: EstiloTexto;
};

export const typography: Typography = {
  hero: { size: 26, weight: '800', lineHeight: 32 },
  h1: { size: 22, weight: '700', lineHeight: 28 },
  h2: { size: 18, weight: '700', lineHeight: 24 },
  h3: { size: 16, weight: '600', lineHeight: 22 },
  body: { size: 14, weight: '400', lineHeight: 20 },
  caption: { size: 12, weight: '400', lineHeight: 16 },
  label: { size: 11, weight: '600', lineHeight: 14 },
};

/** Nomes de conveniência já usados por todas as telas existentes desde a Sprint 2. */
export const fontSize = {
  hero: 26,
  title: 22,
  section: 16,
  cardTitle: 15,
  body: 14,
  secondary: 13,
  meta: 12,
  tiny: 11,
  label: 10,
};

export const fontWeight = {
  regular: '400' as const,
  medium: '600' as const,
  bold: '700' as const,
  black: '800' as const,
};
