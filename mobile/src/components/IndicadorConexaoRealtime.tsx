import React, { useEffect } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import Animated, { useAnimatedStyle, useSharedValue, withRepeat, withTiming } from 'react-native-reanimated';
import { RefreshCw, WifiOff } from 'lucide-react-native';
import { useRealtimeConexao } from '../realtime/RealtimeContext';
import { colors, fontSize, fontWeight, motion, radius, spacing } from '../theme/theme';

/**
 * Sprint 18 (ADR 0022, Fase 4/5) — indicador da conexão SignalR do próprio
 * aparelho com o backend (diferente do chip de conectividade do HeroCard, que
 * mostra se o EQUIPAMENTO/central está se comunicando — ver ADR 0020/Sprint 17).
 * Silencioso quando tudo está bem ("conectado"/"desconectado"/"conectando" não
 * rendem nada) — só aparece quando há algo que o morador precisa saber, nunca
 * com vocabulário técnico ("WebSocket", "SignalR").
 */
export function IndicadorConexaoRealtime() {
  const { estadoConexao, reconectarManualmente } = useRealtimeConexao();

  if (estadoConexao === 'reconectando') {
    return <BannerReconectando />;
  }

  if (estadoConexao === 'sem-comunicacao') {
    return (
      <View style={styles.semComunicacao}>
        <WifiOff size={14} color={colors.warn} />
        <Text style={styles.semComunicacaoTexto}>Sem comunicação com o servidor</Text>
        <Pressable onPress={reconectarManualmente} style={styles.tentarNovamenteBtn} accessibilityRole="button">
          <Text style={styles.tentarNovamenteTexto}>Tentar novamente</Text>
        </Pressable>
      </View>
    );
  }

  return null;
}

function BannerReconectando() {
  const opacidade = useSharedValue(0.5);

  useEffect(() => {
    opacidade.value = withRepeat(withTiming(1, { duration: motion.duration.ambient }), -1, true);
  }, [opacidade]);

  const estiloAnimado = useAnimatedStyle(() => ({ opacity: opacidade.value }));

  return (
    <Animated.View style={[styles.reconectando, estiloAnimado]}>
      <RefreshCw size={13} color={colors.sub} />
      <Text style={styles.reconectandoTexto}>Reconectando...</Text>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  reconectando: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: spacing.xs,
  },
  reconectandoTexto: { color: colors.sub, fontSize: fontSize.tiny, fontWeight: fontWeight.medium },
  semComunicacao: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderRadius: radius.md,
    backgroundColor: colors.warnDim,
    borderWidth: 1,
    borderColor: colors.warnLine,
    marginBottom: spacing.md,
  },
  semComunicacaoTexto: { flex: 1, color: colors.warn, fontSize: fontSize.tiny, fontWeight: fontWeight.medium },
  tentarNovamenteBtn: { paddingHorizontal: spacing.xs, paddingVertical: 2 },
  tentarNovamenteTexto: { color: colors.warn, fontSize: fontSize.tiny, fontWeight: fontWeight.bold, textDecorationLine: 'underline' },
});
