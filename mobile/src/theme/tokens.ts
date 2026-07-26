/**
 * Fonte única de verdade do Design System (ADR 0019, UX001). Nenhum componente deve
 * usar valores de espaçamento/cor/tipografia/animação soltos — sempre importar
 * daqui (via `theme.ts`). Cada categoria vive no próprio arquivo — este módulo só
 * agrega.
 */
export { colors, type Colors } from './colors';
export { spacing, type Spacing } from './spacing';
export { typography, fontSize, fontWeight, type Typography } from './typography';
export { radius, borderRadius, type BorderRadius } from './borders';
export { motion, animation, type Animation } from './animations';
export { shadow, type Shadow } from './shadows';

export const opacity = {
  disabled: 0.5,
  pressed: 0.85,
  overlay: 0.6,
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
