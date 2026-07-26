import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ChevronRight } from 'lucide-react-native';
import { colors, fontSize, fontWeight, spacing } from '../theme/theme';

interface Props {
  title: string;
  actionLabel?: string;
  onPressAction?: () => void;
}

/** Sprint 16 (ADR 0019, UX001) — título de seção com link "Ver todos" opcional. Nunca esconde caminho: se existe mais conteúdo, o link para ele é sempre visível. */
export function SectionHeader({ title, actionLabel, onPressAction }: Props) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>{title}</Text>
      {actionLabel && onPressAction ? (
        <Pressable onPress={onPressAction} style={styles.action} hitSlop={8}>
          <Text style={styles.actionLabel}>{actionLabel}</Text>
          <ChevronRight size={14} color={colors.sub} />
        </Pressable>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: spacing.xl,
    marginBottom: spacing.md,
  },
  title: { fontSize: fontSize.section, fontWeight: fontWeight.bold, color: colors.text },
  action: { flexDirection: 'row', alignItems: 'center', gap: 2 },
  actionLabel: { color: colors.sub, fontSize: fontSize.meta, fontWeight: fontWeight.medium },
});
