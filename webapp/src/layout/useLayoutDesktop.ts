import { useWindowDimensions } from 'react-native';
import { layout } from '../theme/theme';

/** Web App — `true` quando a janela do navegador é larga o bastante para a barra lateral (ver `theme/layout.ts`). */
export function useLayoutDesktop(): boolean {
  const { width } = useWindowDimensions();
  return width >= layout.breakpoints.desktop;
}
