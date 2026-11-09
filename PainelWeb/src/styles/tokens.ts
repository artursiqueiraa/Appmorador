/**
 * Identidade Akteck Seg — fonte única de tokens visuais do Painel (ADR 0035).
 *
 * Até a Sprint 22 estes valores espelhavam 1:1 o app mobile (verde #25C98D como marca).
 * Agora a marca tem cor própria (azul cobalto) e o verde volta a significar só
 * "tudo certo": cor de marca e cor de status nunca se confundem. Todos os pares
 * texto/superfície foram medidos (WCAG AA ≥ 4.5:1; bordas de campo ≥ 3:1).
 */
export const colors = {
  primary: '#4C86FF',
  primaryDark: '#3A6FE0',
  primaryLight: '#7AA6FF',
  onPrimary: '#0A0D12',
  primarySoft: 'rgba(76,134,255,0.14)',
  background: '#0A0D12',
  surface: '#11161D',
  surfaceAlt: '#161C25',
  surfaceHover: '#1C2430',
  textPrimary: '#EEF2F6',
  textSecondary: '#A3AEBB',
  textMuted: '#8592A3',
  success: '#34C77B',
  successSoft: 'rgba(52,199,123,0.14)',
  warning: '#F2A93B',
  warningSoft: 'rgba(242,169,59,0.14)',
  onWarning: '#1A1200',
  error: '#F26464',
  errorSoft: 'rgba(242,100,100,0.14)',
  info: '#2BC4D9',
  border: '#232C38',
  borderStrong: '#5D6A7B',
} as const;

export const colorsLight = {
  primary: '#1F5FE0',
  primaryDark: '#184CB5',
  primaryLight: '#4C86FF',
  onPrimary: '#FFFFFF',
  primarySoft: 'rgba(31,95,224,0.10)',
  background: '#F4F6F9',
  surface: '#FFFFFF',
  surfaceAlt: '#EEF1F5',
  surfaceHover: '#E6EAF0',
  textPrimary: '#0D1320',
  textSecondary: '#485465',
  textMuted: '#626E7F',
  success: '#157A47',
  successSoft: 'rgba(21,122,71,0.10)',
  warning: '#9A5F00',
  warningSoft: 'rgba(154,95,0,0.10)',
  onWarning: '#FFFFFF',
  error: '#C2302B',
  errorSoft: 'rgba(194,48,43,0.10)',
  info: '#0A7489',
  border: '#E1E6EC',
  borderStrong: '#8592A3',
} as const;

export type Paleta = { [K in keyof typeof colors]: string };

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
  sm: 6,
  md: 8,
  lg: 12,
  xl: 16,
  full: 999,
} as const;

/** Escala fixa de razão ~1.2 (telas de trabalho: densidade antes de tamanho). */
export const typography = {
  fontFamily: '"Geist", "Segoe UI", system-ui, sans-serif',
  fontFamilyMono: '"Geist Mono", ui-monospace, "Cascadia Mono", monospace',
  display: { size: 28, weight: 700, lineHeight: '34px' },
  h1: { size: 23, weight: 700, lineHeight: '30px' },
  h2: { size: 19, weight: 600, lineHeight: '26px' },
  h3: { size: 16, weight: 600, lineHeight: '22px' },
  body: { size: 14, weight: 400, lineHeight: '21px' },
  caption: { size: 12, weight: 400, lineHeight: '16px' },
  label: { size: 12, weight: 500, lineHeight: '16px' },
} as const;

export const layout = {
  sidebarWidth: 248,
  headerHeight: 64,
  contentMaxWidth: 1360,
  controlHeight: 38,
} as const;

export const motion = {
  micro: '120ms',
  short: '200ms',
  easing: 'cubic-bezier(0.2, 0, 0, 1)',
} as const;
