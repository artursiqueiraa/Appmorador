import { createTheme, type ThemeOptions } from '@mui/material/styles';
import { borderRadius, colors, colorsLight, layout, motion, spacing, typography, type Paleta } from './tokens';

declare module '@mui/material/styles' {
  interface Palette {
    /** Paleta completa Akteck Seg (tokens.ts), para o que a paleta MUI não cobre: superfícies alternativas, tons "soft" de status, bordas de campo. */
    marca: Paleta;
  }
  interface PaletteOptions {
    marca?: Paleta;
  }
}

function buildTheme(mode: 'light' | 'dark'): ThemeOptions {
  const p: Paleta = mode === 'dark' ? colors : colorsLight;
  const anelFoco = { outline: `2px solid ${p.primary}`, outlineOffset: 2 };

  return {
    palette: {
      mode,
      marca: p,
      primary: { main: p.primary, dark: p.primaryDark, light: p.primaryLight, contrastText: p.onPrimary },
      background: { default: p.background, paper: p.surface },
      text: { primary: p.textPrimary, secondary: p.textSecondary, disabled: p.textMuted },
      success: { main: p.success },
      warning: { main: p.warning, contrastText: p.onWarning },
      error: { main: p.error },
      info: { main: p.info },
      divider: p.border,
      action: { hover: p.surfaceHover, selected: p.primarySoft },
    },
    shape: { borderRadius: borderRadius.md },
    spacing: spacing.sm,
    typography: {
      fontFamily: typography.fontFamily,
      fontSize: typography.body.size,
      h1: { fontSize: typography.h1.size, fontWeight: typography.h1.weight, lineHeight: typography.h1.lineHeight },
      h2: { fontSize: typography.h2.size, fontWeight: typography.h2.weight, lineHeight: typography.h2.lineHeight },
      h3: { fontSize: typography.h3.size, fontWeight: typography.h3.weight, lineHeight: typography.h3.lineHeight },
      h4: { fontSize: typography.h3.size, fontWeight: typography.h3.weight },
      h5: { fontSize: typography.body.size, fontWeight: 600 },
      h6: { fontSize: typography.body.size, fontWeight: 600 },
      body1: { fontSize: typography.body.size, lineHeight: typography.body.lineHeight },
      body2: { fontSize: typography.caption.size + 1, lineHeight: '19px' },
      caption: { fontSize: typography.caption.size, lineHeight: typography.caption.lineHeight },
      overline: { fontSize: typography.label.size, fontWeight: typography.label.weight, textTransform: 'none', letterSpacing: 0 },
      button: { textTransform: 'none', fontWeight: 600, letterSpacing: 0 },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          'html, body': { overflowX: 'clip' },
          body: { backgroundColor: p.background, fontVariantNumeric: 'tabular-nums' },
          ':focus-visible': anelFoco,
          '@media (prefers-reduced-motion: reduce)': {
            '*, *::before, *::after': { transitionDuration: '0.01ms !important', animationDuration: '0.01ms !important' },
          },
        },
      },
      MuiButtonBase: { defaultProps: { disableRipple: true } },
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: {
            minHeight: layout.controlHeight,
            borderRadius: borderRadius.md,
            paddingInline: spacing.lg,
            transition: `background-color ${motion.micro} ${motion.easing}, border-color ${motion.micro} ${motion.easing}`,
            '&.Mui-focusVisible': anelFoco,
          },
          outlined: { borderColor: p.borderStrong, color: p.textPrimary, '&:hover': { borderColor: p.textSecondary, backgroundColor: p.surfaceHover } },
          text: { color: p.textPrimary, '&:hover': { backgroundColor: p.surfaceHover } },
        },
      },
      MuiIconButton: {
        styleOverrides: {
          root: { borderRadius: borderRadius.md, width: 40, height: 40, '&.Mui-focusVisible': anelFoco },
        },
      },
      MuiPaper: {
        defaultProps: { elevation: 0 },
        styleOverrides: { root: { backgroundImage: 'none', borderRadius: borderRadius.lg } },
      },
      MuiCard: {
        defaultProps: { variant: 'outlined' },
        styleOverrides: { root: { borderRadius: borderRadius.lg, borderColor: p.border, backgroundColor: p.surface } },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            borderRadius: borderRadius.md,
            backgroundColor: p.surface,
            '& .MuiOutlinedInput-notchedOutline': { borderColor: p.borderStrong },
            '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: p.textSecondary },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: p.primary, borderWidth: 2 },
          },
          input: { paddingBlock: 10 },
        },
      },
      MuiInputLabel: { styleOverrides: { root: { color: p.textSecondary } } },
      MuiChip: {
        styleOverrides: {
          root: { borderRadius: borderRadius.sm, fontWeight: 500 },
          sizeSmall: { height: 24 },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: { borderColor: p.border, paddingBlock: 10 },
          head: { color: p.textSecondary, fontWeight: 500, fontSize: typography.label.size, backgroundColor: p.surfaceAlt },
        },
      },
      MuiTableRow: {
        styleOverrides: { root: { '&.MuiTableRow-hover:hover': { backgroundColor: p.surfaceHover } } },
      },
      MuiListItemButton: {
        styleOverrides: {
          root: {
            borderRadius: borderRadius.md,
            minHeight: 40,
            '&.Mui-selected': { backgroundColor: p.primarySoft, color: p.textPrimary },
            '&.Mui-selected:hover': { backgroundColor: p.primarySoft },
            '&.Mui-focusVisible': anelFoco,
          },
        },
      },
      MuiDrawer: { styleOverrides: { paper: { backgroundColor: p.surfaceAlt, borderColor: p.border } } },
      MuiAppBar: { styleOverrides: { root: { backgroundColor: p.background, backgroundImage: 'none' } } },
      MuiDialog: { styleOverrides: { paper: { border: `1px solid ${p.border}` } } },
      MuiTooltip: {
        styleOverrides: { tooltip: { backgroundColor: p.textPrimary, color: p.background, fontSize: typography.caption.size } },
      },
      MuiTab: { styleOverrides: { root: { textTransform: 'none', fontWeight: 500, minHeight: 44 } } },
    },
  };
}

export const lightTheme = createTheme(buildTheme('light'));
export const darkTheme = createTheme(buildTheme('dark'));
