import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import type { LucideIcon } from 'lucide-react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

interface Props {
  icon: LucideIcon;
  color: string;
  title: string;
  meta: string;
}

/**
 * Sprint 16 (ADR 0019, UX001) — card de evento com ícone, título, subtítulo e tempo (Atividade recente, Histórico).
 * Sprint 18 (ADR 0022, Regra 5) — memoizado: a lista de Atividade recente não deve
 * re-renderizar todos os cards quando o HeroCard atualiza por causa de um snapshot novo.
 */
export const ActivityCard = React.memo(function ActivityCard({ icon: Icon, color, title, meta }: Props) {
  return (
    <View style={styles.container}>
      <View style={styles.iconWrap}>
        <Icon size={17} color={color} />
      </View>
      <View style={styles.textWrap}>
        <Text style={styles.title} numberOfLines={1}>
          {title}
        </Text>
        <Text style={styles.meta}>{meta}</Text>
      </View>
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md - 1,
    borderBottomWidth: 1,
    borderBottomColor: colors.lineSoft,
  },
  iconWrap: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  textWrap: { flex: 1, minWidth: 0 },
  title: { color: colors.text, fontSize: fontSize.cardTitle - 1.5, fontWeight: fontWeight.medium },
  meta: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
});
