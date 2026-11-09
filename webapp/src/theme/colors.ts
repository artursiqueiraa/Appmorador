/**
 * Identidade Akteck Seg (ADR 0038) — paleta do protótipo original do produto ("Segurança
 * Conectada", UX001): base grafite-azulada (não preto puro) e semântica de status como
 * assinatura — verde = protegido/ação, âmbar = aberto/atenção, vermelho = disparo. Substitui o
 * verde neon do ADR 0037 no Web App, a pedido do usuário. Único ajuste sobre o protótipo:
 * `mute` subiu de #5C6875 (3.0:1) para #8592A3 (5.4:1) para passar em contraste AA.
 * Nenhum componente usa hex literal; sempre importar daqui.
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
  /** Borda de campo de formulário: ≥ 3:1 sobre `surface` (WCAG 1.4.11). */
  borderStrong: string;
  overlay: string;
};

/** Cor da marca: ação principal, seleção e navegação ativa. Nunca usada para status. */
type ColorsMarca = {
  marca: string;
  marcaDim: string;
  marcaLine: string;
  onMarca: string;
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

export const colors: Colors & ColorsMarca & ColorsLegado = {
  // Oficiais
  primary: '#25C98D',
  primaryDark: '#1CA576',
  primaryLight: '#4FE0AC',
  background: '#0A0E13',
  surface: '#141C25',
  surfaceElevated: '#1A2430',
  textPrimary: '#F2F6FA',
  textSecondary: '#93A1B1',
  textMuted: '#8592A3',
  success: '#25C98D',
  warning: '#F5A524',
  error: '#FF4D4D',
  info: '#3DD6C4',
  border: '#26313E',
  borderStrong: '#627083',
  overlay: 'rgba(5,7,10,0.72)',

  // Marca = verde de ação do protótipo
  marca: '#25C98D',
  marcaDim: 'rgba(37,201,141,0.12)',
  marcaLine: 'rgba(37,201,141,0.35)',
  onMarca: '#062015',

  // Legado (nomes usados por todas as telas)
  bg: '#0A0E13',
  bg2: '#0C1219',
  surface2: '#1A2430',
  line: '#26313E',
  lineSoft: '#1E2833',
  text: '#F2F6FA',
  sub: '#93A1B1',
  mute: '#8592A3',
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

/** Efeitos do protótipo: discretos — sombra suave, sem brilho neon. Botão principal em verde sólido. */
export const efeitos = {
  brilhoMarca: '0 8px 22px rgba(37,201,141,0.18)',
  brilhoSuave: '0 1px 2px rgba(0,0,0,0.25)',
  brilhoAtivo: 'inset 0 0 0 1px rgba(37,201,141,0.35)',
  degradeMarca: ['#25C98D', '#25C98D'] as const,
  degradeFundo: ['#0C1219', '#0A0E13'] as const,
};
