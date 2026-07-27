import React, { useEffect, useRef } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import Animated, { useAnimatedStyle, useSharedValue, withRepeat, withSequence, withTiming } from 'react-native-reanimated';
import { ShieldAlert, ShieldCheck, ShieldOff, Wifi, WifiOff } from 'lucide-react-native';
import { colors, fontSize, fontWeight, iconSize, motion, radius, spacing } from '../theme/theme';

export type HeroStatus = 'protegido' | 'atencao' | 'desarmado';

export type EstadoConectividade = 'conectado' | 'atencao' | 'offline';

export interface Conectividade {
  estado: EstadoConectividade;
  label: string;
}

interface Props {
  status: HeroStatus;
  titulo: string;
  subtitulo: string;
  /** Sprint 17 (ADR 0020) — "seu alarme está armado" e "seu equipamento está se comunicando agora" são coisas diferentes; nunca misturar no mesmo texto. */
  conectividade?: Conectividade;
  children?: React.ReactNode;
}

const VISUAL: Record<HeroStatus, { cor: string; corDim: string; corLine: string; Icone: typeof ShieldCheck }> = {
  protegido: { cor: colors.safe, corDim: colors.safeDim, corLine: colors.safeLine, Icone: ShieldCheck },
  atencao: { cor: colors.danger, corDim: colors.dangerDim, corLine: colors.dangerLine, Icone: ShieldAlert },
  desarmado: { cor: colors.warn, corDim: colors.warnDim, corLine: colors.warnLine, Icone: ShieldOff },
};

const VISUAL_CONECTIVIDADE: Record<EstadoConectividade, { cor: string; Icone: typeof Wifi }> = {
  conectado: { cor: colors.safe, Icone: Wifi },
  atencao: { cor: colors.warn, Icone: Wifi },
  offline: { cor: colors.mute, Icone: WifiOff },
};

/**
 * Sprint 16 (ADR 0019, UX001) — card de status principal do Início. O anel "respira"
 * só quando protegido (indica "vigilância contínua", nunca decorativo); em
 * atenção/desarmado o anel fica parado — a ausência de movimento também comunica
 * algo (sistema não está monitorando ativamente).
 *
 * Sprint 18 (ADR 0022, Fase 1) — pulso sutil (scale, ≤300ms) toda vez que o
 * conteúdo relevante muda (título/subtítulo/conectividade), sinalizando "isto
 * acabou de atualizar" sem reconstruir o card inteiro. Memoizado (Regra 5): só
 * re-renderiza quando essas props realmente mudam, nunca por causa de estado de
 * componentes vizinhos (Timeline, Painel, indicador de conexão).
 */
export const HeroCard = React.memo(function HeroCard({ status, titulo, subtitulo, conectividade, children }: Props) {
  const visual = VISUAL[status];
  const respirando = status === 'protegido';
  const escala = useSharedValue(1);
  const opacidade = useSharedValue(0.5);
  const escalaAtualizacao = useSharedValue(1);
  const primeiraRenderizacaoRef = useRef(true);

  useEffect(() => {
    if (respirando) {
      escala.value = withRepeat(withTiming(1.14, { duration: motion.duration.ambient }), -1, true);
      opacidade.value = withRepeat(withTiming(0.15, { duration: motion.duration.ambient }), -1, true);
    } else {
      escala.value = withTiming(1, { duration: motion.duration.fast });
      opacidade.value = withTiming(0.3, { duration: motion.duration.fast });
    }
  }, [respirando, escala, opacidade]);

  useEffect(() => {
    if (primeiraRenderizacaoRef.current) {
      primeiraRenderizacaoRef.current = false;
      return;
    }
    escalaAtualizacao.value = withSequence(withTiming(1.02, { duration: 120 }), withTiming(1, { duration: 150 }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [titulo, subtitulo, conectividade?.label]);

  const anelStyle = useAnimatedStyle(() => ({
    transform: [{ scale: escala.value }],
    opacity: opacidade.value,
  }));

  const atualizacaoStyle = useAnimatedStyle(() => ({
    transform: [{ scale: escalaAtualizacao.value }],
  }));

  return (
    <Animated.View style={[styles.container, { backgroundColor: visual.corDim, borderColor: visual.corLine }, atualizacaoStyle]}>
      <View style={styles.iconeWrap}>
        <Animated.View style={[styles.anel, { borderColor: visual.cor }, anelStyle]} />
        <View style={[styles.icone, { backgroundColor: visual.corDim, borderColor: visual.cor }]}>
          <visual.Icone size={iconSize.xl} color={visual.cor} />
        </View>
      </View>
      <Text style={styles.titulo}>{titulo}</Text>
      <Text style={styles.subtitulo}>{subtitulo}</Text>
      {conectividade
        ? (() => {
            const { cor, Icone: IconeConectividade } = VISUAL_CONECTIVIDADE[conectividade.estado];
            return (
              <View style={styles.conectividadeWrap}>
                <IconeConectividade size={13} color={cor} />
                <Text style={[styles.conectividadeLabel, { color: cor }]}>{conectividade.label}</Text>
              </View>
            );
          })()
        : null}
      {children ? <View style={styles.acoes}>{children}</View> : null}
    </Animated.View>
  );
});

const styles = StyleSheet.create({
  container: {
    borderRadius: radius.xxl,
    borderWidth: 1,
    paddingVertical: spacing.xxl,
    paddingHorizontal: spacing.xl,
    alignItems: 'center',
  },
  iconeWrap: { width: 74, height: 74, alignItems: 'center', justifyContent: 'center', marginBottom: spacing.md },
  anel: { position: 'absolute', width: 74, height: 74, borderRadius: 999, borderWidth: 1 },
  icone: {
    width: 74,
    height: 74,
    borderRadius: 999,
    borderWidth: 1.5,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.hero, fontWeight: fontWeight.black, letterSpacing: 1, textAlign: 'center' },
  subtitulo: { color: colors.sub, fontSize: fontSize.secondary, marginTop: spacing.xs, textAlign: 'center' },
  conectividadeWrap: { flexDirection: 'row', alignItems: 'center', gap: 5, marginTop: spacing.sm },
  conectividadeLabel: { fontSize: fontSize.tiny, fontWeight: fontWeight.medium },
  acoes: { flexDirection: 'row', gap: spacing.sm, marginTop: spacing.lg, width: '100%' },
});
