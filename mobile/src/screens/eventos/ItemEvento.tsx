import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { ShieldAlert, ShieldCheck } from 'lucide-react-native';
import type { EventoResponse } from '../../api/types';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

interface Props {
  evento: EventoResponse;
  /** Sem borda/fundo próprios — usado quando o item já vive dentro de outro card
   * (ex.: dentro de um card do Dashboard), para não empilhar "card dentro de card". */
  compacto?: boolean;
  /** Sprint 18 (ADR 0022, Fase 2) — acabou de chegar via SignalR: entra com Fade+Slide (≤300ms) e ganha o selo "Novo" por alguns segundos. */
  destaqueNovo?: boolean;
}

export const ItemEvento = React.memo(function ItemEvento({ evento, compacto = false, destaqueNovo = false }: Props) {
  const cor = evento.destaque ? colors.danger : colors.safe;
  const Icon = evento.destaque ? ShieldAlert : ShieldCheck;

  const conteudo = (
    <View style={compacto ? styles.cardCompacto : styles.card}>
      <View style={styles.iconWrap}>
        <Icon size={iconSize.sm} color={cor} />
      </View>
      <View style={styles.textWrap}>
        <View style={styles.tituloLinha}>
          <Text style={styles.titulo}>{evento.titulo}</Text>
          {destaqueNovo ? (
            <View style={styles.seloNovo}>
              <Text style={styles.seloNovoTexto}>Novo</Text>
            </View>
          ) : null}
        </View>
        {evento.descricao ? <Text style={styles.descricao}>{evento.descricao}</Text> : null}
        <Text style={styles.tempo}>{formatRelativeTime(evento.ocorridoEmUtc)}</Text>
      </View>
    </View>
  );

  if (!destaqueNovo) {
    return conteudo;
  }

  return <Animated.View entering={FadeInUp.duration(280)}>{conteudo}</Animated.View>;
});

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
  tituloLinha: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs },
  titulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  seloNovo: {
    paddingHorizontal: spacing.xs,
    paddingVertical: 1,
    borderRadius: radius.pill,
    backgroundColor: colors.safeDim,
    borderWidth: 1,
    borderColor: colors.safeLine,
  },
  seloNovoTexto: { color: colors.safe, fontSize: fontSize.label, fontWeight: fontWeight.bold },
  descricao: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  tempo: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 4 },
});
