import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { ShieldAlert, ShieldCheck } from 'lucide-react-native';
import type { EventoResponse } from '../../api/types';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

interface Props {
  evento: EventoResponse;
  /** Sem borda/fundo próprios — usado quando o item já vive dentro de outro card
   * (ex.: dentro de um card do Dashboard), para não empilhar "card dentro de card". */
  compacto?: boolean;
}

export function ItemEvento({ evento, compacto = false }: Props) {
  const cor = evento.destaque ? colors.danger : colors.safe;
  const Icon = evento.destaque ? ShieldAlert : ShieldCheck;

  return (
    <View style={compacto ? styles.cardCompacto : styles.card}>
      <View style={styles.iconWrap}>
        <Icon size={iconSize.sm} color={cor} />
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
  cardCompacto: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.md,
    paddingVertical: spacing.xs,
  },
  iconWrap: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface2,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
  },
  textWrap: { flex: 1 },
  titulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  descricao: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  tempo: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 4 },
});