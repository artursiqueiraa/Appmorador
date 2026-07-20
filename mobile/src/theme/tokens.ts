/**
 * Fonte única de verdade do Design System. Nenhum componente deve usar valores de
 * espaçamento/cor/tipografia/animação soltos — sempre importar daqui (via `theme.ts`).
 * Paleta extraída do mockup original (web/lucide-react): grafite-azulada, dark mode
 * fixo, semântica de status (verde = protegido, âmbar = atenção, vermelho = disparo).
 */
export const colors = {
  bg: '#0A0E13',
  bg2: '#0C1219',
  surface: '#141C25',
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
  danger: '#FF4D4D',
  dangerDim: 'rgba(255,77,77,0.13)',
  dangerLine: 'rgba(255,77,77,0.4)',
  accent: '#3DD6C4',
};

export const radius = {
  sm: 8,
  md: 11,
  lg: 14,
  xl: 16,
  xxl: 22,
  pill: 999,
};

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 18,
  xxl: 24,
};

export const typography = {
  fontSize: {
    hero: 26,
    title: 22,
    section: 16,
    cardTitle: 15,
    body: 14,
    secondary: 13,
    meta: 12,
    tiny: 11,
    label: 10,
  },
  fontWeight: {
    regular: '400' as const,
    medium: '600' as const,
    bold: '700' as const,
    black: '800' as const,
  },
};

/** Toda animação do app usa uma destas durações/curvas — nunca um número solto no componente. */
export const motion = {
  duration: {
    fast: 150,
    base: 300,
    slow: 500,
  },
  easing: {
    standard: [0.4, 0, 0.2, 1] as const,
    decelerate: [0, 0, 0.2, 1] as const,
    accelerate: [0.4, 0, 1, 1] as const,
  },
};

export const opacity = {
  disabled: 0.5,
  pressed: 0.85,
  overlay: 0.6,
};

export const shadow = {
  sm: { shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.2, shadowRadius: 2, elevation: 2 },
  md: { shadowColor: '#000', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.25, shadowRadius: 8, elevation: 4 },
  lg: { shadowColor: '#000', shadowOffset: { width: 0, height: 8 }, shadowOpacity: 0.3, shadowRadius: 16, elevation: 8 },
};

export const zIndex = {
  base: 0,
  overlay: 10,
  modal: 20,
  toast: 30,
};

export const iconSize = {
  sm: 16,
  md: 20,
  lg: 28,
  xl: 34,
};
