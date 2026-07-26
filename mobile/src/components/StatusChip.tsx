import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

export type StatusChipVariant = 'success' | 'warning' | 'error' | 'info' | 'neutral';

interface Props {
  label: string;
  variant?: StatusChipVariant;
  /** Ponto pulsante (ex.: "AO VIVO") — sinaliza algo acontecendo agora, nunca decorativo. */
  pulse?: boolean;
}

const CORES: Record<StatusChipVariant, { bg: string; fg: string }> = {
  success: { bg: colors.safeDim, fg: colors.safe },
  warning: { bg: colors.warnDim, fg: colors.warn },
  error: { bg: colors.dangerDim, fg: colors.danger },
  info: { bg: 'rgba(61,214,196,0.14)', fg: colors.accent },
  neutral: { bg: colors.surface2, fg: colors.sub },
};

/** Sprint 16 (ADR 0019, UX001) — badge de status (Online/Offline/AO VIVO/...). */
export function StatusChip({ label, variant = 'neutral', pulse }: Props) {
  const cor = CORES[variant];

  return (
    <View style={[styles.container, { backgroundColor: cor.bg }]}>
      {pulse ? <View style={[styles.dot, { backgroundColor: cor.fg }]} /> : null}
      <Text style={[styles.label, { color: cor.fg }]}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: spacing.xs,
    paddingHorizontal: spacing.sm + 1,
    paddingVertical: 4,
    borderRadius: radius.pill,
  },
  dot: { width: 6, height: 6, borderRadius: radius.pill },
  label: { fontSize: fontSize.label, fontWeight: fontWeight.bold, letterSpacing: 0.3 },
});
