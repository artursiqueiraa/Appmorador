import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Clock } from 'lucide-react-native';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

interface Props {
  ultimoEvento?: string | null;
  ultimoEventoEmUtc?: string | null;
}

export function CardUltimaAtividade({ ultimoEvento, ultimoEventoEmUtc }: Props) {
  return (
    <View style={styles.card}>
      <Clock size={iconSize.md} color={colors.mute} />
      <View style={styles.textWrap}>
        <Text style={styles.titulo}>Última atividade</Text>
        <Text style={styles.descricao}>{ultimoEvento ?? 'Nenhum evento recente'}</Text>
        {ultimoEventoEmUtc ? <Text style={styles.tempo}>{formatRelativeTime(ultimoEventoEmUtc)}</Text> : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.md,
    padding: spacing.lg,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.lineSoft,
    marginBottom: spacing.md,
  },
  textWrap: { flex: 1 },
  titulo: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  descricao: { color: colors.text, fontSize: fontSize.body, marginTop: 2 },
  tempo: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 4 },
});
