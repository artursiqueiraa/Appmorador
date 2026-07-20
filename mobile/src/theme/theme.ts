/**
 * Barril do Design System — importa tudo de `tokens.ts` (fonte única de verdade) e
 * reexpõe. `colors`/`spacing`/`radius`/`fontSize`/`fontWeight` são mantidos como
 * nomes de conveniência (compatibilidade com telas já existentes); código novo pode
 * importar `typography`/`motion`/`shadow`/`opacity`/`zIndex`/`iconSize` diretamente.
 */
import { colors, radius, spacing, typography, motion, shadow, opacity, zIndex, iconSize } from './tokens';

export const fontSize = typography.fontSize;
export const fontWeight = typography.fontWeight;

export { colors, radius, spacing, typography, motion, shadow, opacity, zIndex, iconSize };
