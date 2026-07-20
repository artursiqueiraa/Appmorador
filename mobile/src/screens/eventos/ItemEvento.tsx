import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { ShieldAlert, ShieldCheck } from 'lucide-react-native';
import type { EventoResponse } from '../../api/types';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

interface Props {
  evento: EventoResponse;
}

export function ItemEvento({ evento }: Props) {
  const cor = evento.destaque ? colors.danger : colors.safe;
  const Icon = evento.destaque ? ShieldAlert : ShieldCheck;

  return (
    <View style={styles.card}>
      <View style={[styles.iconWrap, { borderColor: cor }]}>
        <Icon size={iconSize.md} color={cor} />
      </View>
      <View style={styles.textWrap}>
        <Text style={styles.titulo}>{evento.titulo}</Text>
        {evento.descricao ? <Text style={styles.descricao}>{evento.descricao}</Text> : null}
        <Text style={styles.tempo}>{formatRelativeTime(evento.ocorridoEmUtc)}</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
  },
  iconWrap: {
    width: 38,
    height: 38,
    borderRadius: 999,
    borderWidth: 1.5,
    alignItems: 'center',
    justifyContent: 'center',
  },
  textWrap: { flex: 1 },
  titulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  descricao: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  tempo: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 4 },
});