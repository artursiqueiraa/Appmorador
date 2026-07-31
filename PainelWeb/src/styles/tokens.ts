/**
 * Sprint 22A — Design Tokens mapeados 1:1 com o app mobile (`mobile/src/theme/`).
 * Mesmos valores exatos (cores/espaçamento/tipografia/raio) — só reorganizados
 * para o formato de tema do Material UI (ver `styles/muiTheme.ts`).
 */
export const colors = {
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
} as const;

/** Sprint 22A — mesmos tons oficiais, ajustados para fundo claro (o mobile só tem tema escuro). */
export const colorsLight = {
  primary: '#1CA576',
  primaryDark: '#158A61',
  primaryLight: '#25C98D',
  background: '#F7F9FB',
  surface: '#FFFFFF',
  surfaceElevated: '#F0F3F6',
  textPrimary: '#0A0E13',
  textSecondary: '#4A5568',
  textMuted: '#8896A6',
  success: '#1CA576',
  warning: '#B87A00',
  error: '#D32F2F',
  info: '#0E8F82',
  border: '#DCE3EA',
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  xxxl: 32,
} as const;

export const borderRadius = {
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  full: 999,
} as const;

export const typography = {
  hero: { size: 26, weight: 800, lineHeight: '32px' },
  h1: { size: 22, weight: 700, lineHeight: '28px' },
  h2: { size: 18, weight: 700, lineHeight: '24px' },
  h3: { size: 16, weight: 600, lineHeight: '22px' },
  body: { size: 14, weight: 400, lineHeight: '20px' },
  caption: { size: 12, weight: 400, lineHeight: '16px' },
  label: { size: 11, weight: 600, lineHeight: '14px' },
} as const;
