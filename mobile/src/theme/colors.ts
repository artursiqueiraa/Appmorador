/**
 * Sprint 16 (ADR 0019, UX001) — paleta oficial. Os valores são os mesmos já usados
 * pelo app desde a Sprint 2 (confirmados idênticos ao protótipo UX001 anexado) —
 * nenhuma cor mudou, só passaram a ter também os nomes semânticos exigidos pelo
 * Design System oficial (`primary`/`background`/`textPrimary`/...), lado a lado com
 * os nomes já usados por todas as telas existentes (`safe`/`bg`/`text`/...). Nenhum
 * componente — novo ou antigo — pode usar um hex literal; sempre importar daqui.
 */
export type Colors = {
  primary: string;
  primaryDark: string;
  primaryLight: string;
  background: string;
  surface: string;
  surfaceElevated: string;
  textPrimary: string;
  textSecondary: string;
  textMuted: string;
  success: string;
  warning: string;
  error: string;
  info: string;
  border: string;
  overlay: string;
};

/** Nomes de conveniência já usados por todas as telas desde a Sprint 2 — nunca removidos, só complementados. */
type ColorsLegado = {
  bg: string;
  bg2: string;
  surface2: string;
  line: string;
  lineSoft: string;
  text: string;
  sub: string;
  mute: string;
  safe: string;
  safeDim: string;
  safeLine: string;
  warn: string;
  warnDim: string;
  warnLine: string;
  danger: string;
  dangerDim: string;
  dangerLine: string;
  accent: string;
};

export const colors: Colors & ColorsLegado = {
  // Oficiais (Colors, UX001)
  primary: '#25C98D',
  primaryDark: '#1CA576',
  primaryLight: '#4FE0AC',
  background: '#0A0E13',
  surface: '#141C25',
  surfaceElevated: '#1A2430',
  textPrimary: '#F2F6FA',
  textSecondary: '#93A1B1',
  textMuted: '#5C6875',
  success: '#25C98D',
  warning: '#F5A524',
  error: '#FF4D4D',
  info: '#3DD6C4',
  border: '#26313E',
  overlay: 'rgba(5,7,10,0.6)',

  // Legado (nomes já usados por todas as telas existentes)
  bg: '#0A0E13',
  bg2: '#0C1219',
  surface2: '#1A2430',
  line: '#26313E',
  lineSoft: '#1E2833',
  text: '#F2F6FA',
  sub: '#93A1B1',
  mute: '#5C6875',
  safe: '#25C98D',
  safeDim: 'rgba(37,201,141,0.12)',
  safeLine: 'rgba(37,201,141,0.35)',
  warn: '#F5A524',
  warnDim: 'rgba(245,165,36,0.12)',
  warnLine: 'rgba(245,165,36,0.35)',
  danger: '#FF4D4D',
  dangerDim: 'rgba(255,77,77,0.13)',
  dangerLine: 'rgba(255,77,77,0.4)',
  accent: '#3DD6C4',
};
