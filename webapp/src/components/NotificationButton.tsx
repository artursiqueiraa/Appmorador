import React from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import { Bell } from 'lucide-react-native';
import { colors, radius } from '../theme/theme';

interface Props {
  hasUnread?: boolean;
  onPress: () => void;
}

/** Sprint 16 (ADR 0019, UX001) — botão de notificações com badge (Início). Leva para a Atividade — não existe um inbox de notificação push separado (fora de escopo, ver ADR 0017). */
export function NotificationButton({ hasUnread, onPress }: Props) {
  return (
    <Pressable onPress={onPress} style={styles.container} accessibilityLabel="Ver atividade recente">
      <Bell size={18} color={colors.sub} />
      {hasUnread ? <View style={styles.badge} /> : null}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'relative',
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badge: {
    position: 'absolute',
    top: 8,
    right: 8,
    width: 7,
    height: 7,
    borderRadius: radius.pill,
    backgroundColor: colors.danger,
    borderWidth: 2,
    borderColor: colors.surface,
  },
});
