import React, { useEffect } from 'react';
import { Linking, Pressable, StyleSheet, Text, View } from 'react-native';
import Animated, { SlideInDown, useAnimatedStyle, useSharedValue, withRepeat, withTiming } from 'react-native-reanimated';
import { Siren, Video, X } from 'lucide-react-native';
import { colors, fontSize, fontWeight, iconSize, motion, radius, spacing } from '../../theme/theme';
import { PrimaryButton } from '../../components/PrimaryButton';

interface Props {
  onClose: () => void;
}

/**
 * Sprint 16 (ADR 0019, UX001) — alerta de disparo em tela cheia. Diferente do
 * protótipo original, não mostra uma linha do tempo de vídeo pré/pós-disparo — o
 * backend só captura uma única imagem no momento do disparo (decisão de MVP da Fase
 * 2), nunca um buffer contínuo de vídeo. Mostrar uma barra de "scrubbing" fingindo
 * esse buffer seria simular uma capacidade que não existe — registrado como
 * dívida técnica (ver DIVIDA_TECNICA.md) para quando um buffer de vídeo real existir.
 */
export function AlertaDisparo({ onClose }: Props) {
  const flash = useSharedValue(1);

  useEffect(() => {
    flash.value = withRepeat(withTiming(0.4, { duration: 550 }), -1, true);
  }, [flash]);

  const flashStyle = useAnimatedStyle(() => ({ opacity: flash.value }));

  const ligarEmergencia = () => Linking.openURL('tel:190');

  return (
    <Animated.View entering={SlideInDown.duration(motion.duration.base)} style={styles.container}>
      <View style={styles.header}>
        <View style={styles.headerEsquerda}>
          <Animated.View style={[styles.iconeAlerta, flashStyle]}>
            <Siren size={18} color={colors.danger} />
          </Animated.View>
          <View>
            <Text style={styles.titulo}>ALARME DISPARADO</Text>
            <Text style={styles.subtitulo}>Verifique sua casa agora</Text>
          </View>
        </View>
        <Pressable onPress={onClose} style={styles.fechar} accessibilityLabel="Fechar alerta">
          <X size={18} color={colors.sub} />
        </Pressable>
      </View>

      <View style={styles.preview}>
        <Video size={iconSize.xl} color={colors.mute} />
        <Text style={styles.previewTexto}>Imagem do momento do disparo aparece aqui quando disponível</Text>
      </View>

      <View style={styles.acoesEmergencia}>
        <PrimaryButton label="Ligar 190" onPress={ligarEmergencia} variant="secondary" />
      </View>

      <View style={styles.rodape}>
        <PrimaryButton label="É um alarme falso — desarmar" onPress={onClose} />
      </View>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: colors.bg,
    padding: spacing.xl,
    justifyContent: 'flex-start',
  },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: spacing.lg },
  headerEsquerda: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, flex: 1 },
  iconeAlerta: {
    width: 34,
    height: 34,
    borderRadius: radius.md,
    backgroundColor: colors.dangerDim,
    borderWidth: 1,
    borderColor: colors.dangerLine,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.danger, fontSize: fontSize.cardTitle, fontWeight: fontWeight.black, letterSpacing: 0.5 },
  subtitulo: { color: colors.sub, fontSize: fontSize.tiny, marginTop: 2 },
  fechar: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  preview: {
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.dangerLine,
    backgroundColor: colors.surface,
    height: 190,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    padding: spacing.lg,
  },
  previewTexto: { color: colors.mute, fontSize: fontSize.tiny, textAlign: 'center' },
  acoesEmergencia: { marginTop: spacing.lg },
  rodape: { marginTop: 'auto', paddingTop: spacing.lg },
});
