/**
 * Barril do Design System — importa tudo de `tokens.ts` (fonte única de verdade) e
 * reexpõe. `colors`/`spacing`/`radius`/`fontSize`/`fontWeight`/`motion` são os nomes
 * de conveniência usados por todas as telas desde a Sprint 2; `typography`/
 * `borderRadius`/`animation`/`shadow` são os tokens oficiais do Design System
 * UX001 (ADR 0019) — código novo deve preferir estes.
 */
export {
  colors,
  spacing,
  radius,
  borderRadius,
  typography,
  fontSize,
  fontWeight,
  motion,
  animation,
  shadow,
  opacity,
  zIndex,
  iconSize,
} from './tokens';
