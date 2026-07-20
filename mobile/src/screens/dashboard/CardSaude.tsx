import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Activity } from 'lucide-react-native';
import Animated, { runOnJS, useAnimatedReaction, useSharedValue, withTiming } from 'react-native-reanimated';
import { colors, fontSize, fontWeight, iconSize, motion, radius, spacing } from '../../theme/theme';

interface Props {
  pontuacaoSaude: number;
  protegido: boolean;
}

/** Rótulo amigável sempre acompanha o número — nunca mostrar só o percentual (regra de produto). */
function rotuloSaude(pontuacao: number): string {
  if (pontuacao >= 100) return 'Tudo funcionando normalmente';
  if (pontuacao >= 90) return 'Excelente';
  if (pontuacao >= 60) return 'Atenção';
  return 'Necessita revisão';
}

export function CardSaude({ pontuacaoSaude, protegido }: Props) {
  const [valorExibido, setValorExibido] = useState(0);
  const valorAnimado = useSharedValue(0);

  useEffect(() => {
    // Contagem comunica progresso real (não é decoração) — vai de 0 até a pontuação atual.
    valorAnimado.value = withTiming(pontuacaoSaude, { duration: motion.duration.slow });
  }, [pontuacaoSaude, valorAnimado]);

  useAnimatedReaction(
    () => Math.round(valorAnimado.value),
    (atual, anterior) => {
      if (atual !== anterior) {
        runOnJS(setValorExibido)(atual);
      }
    },
  );

  const corStatus = protegido ? colors.safe : colors.warn;

  return (
    <Animated.View style={styles.card}>
      <View style={[styles.iconWrap, { borderColor: corStatus }]}>
        <Activity size={iconSize.lg} color={corStatus} />
      </View>
      <View style={styles.textWrap}>
        <Text style={styles.valor}>{valorExibido}%</Text>
        <Text style={styles.rotulo}>{rotuloSaude(pontuacaoSaude)}</Text>
      </View>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.lg,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.md,
  },
  iconWrap: {
    width: 56,
    height: 56,
    borderRadius: 999,
    borderWidth: 1.5,
    alignItems: 'center',
    justifyContent: 'center',
  },
  textWrap: { flex: 1 },
  valor: { color: colors.text, fontSize: fontSize.hero, fontWeight: fontWeight.black },
  rotulo: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
});
