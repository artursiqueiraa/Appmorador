import { createTheme, type ThemeOptions } from '@mui/material/styles';
import { borderRadius, colors, colorsLight, spacing } from './tokens';

function buildTheme(mode: 'light' | 'dark'): ThemeOptions {
  const palette = mode === 'dark' ? colors : colorsLight;

  return {
    palette: {
      mode,
      primary: { main: palette.primary, dark: palette.primaryDark, light: palette.primaryLight },
      background: { default: palette.background, paper: palette.surface },
      text: { primary: palette.textPrimary, secondary: palette.textSecondary },
      success: { main: palette.success },
      warning: { main: palette.warning },
      error: { main: palette.error },
      info: { main: palette.info },
      divider: palette.border,
    },
    shape: {
      borderRadius: borderRadius.md,
    },
    spacing: spacing.sm,
    typography: {
      fontFamily: '"Inter", "Roboto", "Segoe UI", sans-serif',
      h1: { fontSize: 26, fontWeight: 800 },
      h2: { fontSize: 22, fontWeight: 700 },
      h3: { fontSize: 18, fontWeight: 700 },
      h4: { fontSize: 16, fontWeight: 600 },
      body1: { fontSize: 14, fontWeight: 400 },
      caption: { fontSize: 12, fontWeight: 400 },
    },
    components: {
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: { root: { textTransform: 'none', borderRadius: borderRadius.sm } },
      },
      MuiPaper: {
        styleOverrides: { root: { borderRadius: borderRadius.lg } },
      },
      MuiCard: {
        styleOverrides: { root: { borderRadius: borderRadius.lg } },
      },
    },
  };
}

export const lightTheme = createTheme(buildTheme('light'));
export const darkTheme = createTheme(buildTheme('dark'));
