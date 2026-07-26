import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { MapPin } from 'lucide-react-native';
import { NotificationButton } from './NotificationButton';
import { colors, fontSize, fontWeight, iconSize, spacing } from '../theme/theme';

interface Props {
  saudacao: string;
  nomePropriedade: string;
  temAtividadeNaoVista?: boolean;
  onPressNotificacoes: () => void;
}

/** Sprint 16 (ADR 0019, UX001) — cabeçalho com nome e propriedade (Início, Ajustes). */
export function ProfileHeader({ saudacao, nomePropriedade, temAtividadeNaoVista, onPressNotificacoes }: Props) {
  return (
    <View style={styles.container}>
      <View>
        <Text style={styles.saudacao}>{saudacao}</Text>
        <View style={styles.linhaPropriedade}>
          <MapPin size={iconSize.sm} color={colors.mute} />
          <Text style={styles.propriedade}>{nomePropriedade}</Text>
        </View>
      </View>
      <NotificationButton hasUnread={temAtividadeNaoVista} onPress={onPressNotificacoes} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: spacing.sm,
    marginBottom: spacing.lg,
  },
  saudacao: { color: colors.sub, fontSize: fontSize.secondary },
  linhaPropriedade: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, marginTop: 3 },
  propriedade: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.bold },
});
