import React from 'react';
import { StyleSheet, View, type StyleProp, type ViewStyle } from 'react-native';
import { colors, efeitos, radius, spacing } from '../../theme/theme';

/** Akteck Seg (ADR 0037) — cartão escuro com borda verde sutil e brilho; `ativo` destaca o selecionado. */
export function CartaoNeon({ children, ativo, style, semPadding }: { children: React.ReactNode; ativo?: boolean; style?: StyleProp<ViewStyle>; semPadding?: boolean }) {
  return <View style={[styles.cartao, !semPadding && styles.padding, ativo && styles.ativo, style]}>{children}</View>;
}

const styles = StyleSheet.create({
  cartao: {
    backgroundColor: colors.surface,
    borderRadius: radius.xl,
    borderWidth: 1,
    borderColor: colors.line,
    boxShadow: efeitos.brilhoSuave,
  },
  padding: { padding: spacing.lg },
  ativo: { borderColor: colors.marca, backgroundColor: colors.marcaDim, boxShadow: efeitos.brilhoAtivo },
});
