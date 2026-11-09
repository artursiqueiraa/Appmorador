import React from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';
import { Siren } from 'lucide-react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

interface Props {
  label: string;
  onPress: () => void;
}

/**
 * Sprint 16 (ADR 0019, UX001) — botão de simulação, só existe em build de
 * desenvolvimento (`__DEV__`). Nunca aparece numa build de produção — a checagem
 * fica dentro do próprio componente, não depende de quem o usa lembrar de escondê-lo.
 */
export function DemoButton({ label, onPress }: Props) {
  if (!__DEV__) {
    return null;
  }

  return (
    <Pressable onPress={onPress} style={styles.container}>
      <Siren size={16} color={colors.warn} />
      <Text style={styles.label}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    marginTop: spacing.lg,
    paddingVertical: spacing.md - 1,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderStyle: 'dashed',
    borderColor: colors.warnLine,
    backgroundColor: colors.warnDim,
  },
  label: { color: colors.warn, fontSize: fontSize.secondary, fontWeight: fontWeight.bold },
});
