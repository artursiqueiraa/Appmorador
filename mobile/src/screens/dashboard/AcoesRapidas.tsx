import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Lock, Unlock } from 'lucide-react-native';
import Animated, { useAnimatedStyle, useSharedValue, withTiming } from 'react-native-reanimated';
import { ServicoFeedbackTatil } from '../../services/ServicoFeedbackTatil';
import { colors, fontSize, fontWeight, iconSize, motion, radius, spacing } from '../../theme/theme';

interface Props {
  armado: boolean;
  onChange: (armado: boolean) => void;
}

/**
 * Nesta Sprint os botões são só visuais — ainda não existe comando real à central JFL
 * (isso é uma funcionalidade nova, de uma Sprint futura). Os handlers já ficam
 * nomeados e isolados aqui para que a chamada real substitua só o corpo da função,
 * sem precisar reescrever a tela.
 */
export function AcoesRapidas({ armado, onChange }: Props) {
  const handleArmar = () => {
    ServicoFeedbackTatil.impactLight();
    onChange(true);
  };

  const handleDesarmar = () => {
    ServicoFeedbackTatil.impactLight();
    onChange(false);
  };

  return (
    <View style={styles.row}>
      <BotaoAcao
        ativo={armado}
        onPress={handleArmar}
        icone={<Lock size={iconSize.md} color={armado ? colors.safe : colors.sub} />}
        label="Armar"
        corAtiva="safe"
      />
      <BotaoAcao
        ativo={!armado}
        onPress={handleDesarmar}
        icone={<Unlock size={iconSize.md} color={!armado ? colors.warn : colors.sub} />}
        label="Desarmar"
        corAtiva="warn"
      />
    </View>
  );
}

function BotaoAcao({
  ativo,
  onPress,
  icone,
  label,
  corAtiva,
}: {
  ativo: boolean;
  onPress: () => void;
  icone: React.ReactNode;
  label: string;
  corAtiva: 'safe' | 'warn';
}) {
  const escala = useSharedValue(1);
  const animatedStyle = useAnimatedStyle(() => ({ transform: [{ scale: escala.value }] }));

  return (
    <Animated.View style={[styles.btnWrap, animatedStyle]}>
      <Pressable
        onPressIn={() => {
          // Feedback de toque imediato — comunica que o app registrou a intenção.
          escala.value = withTiming(0.96, { duration: motion.duration.fast });
        }}
        onPressOut={() => {
          escala.value = withTiming(1, { duration: motion.duration.fast });
        }}
        onPress={onPress}
        style={[
          styles.btn,
          ativo && (corAtiva === 'safe' ? styles.btnAtivoSafe : styles.btnAtivoWarn),
        ]}
      >
        {icone}
        <Text style={[styles.label, ativo && styles.labelAtivo]}>{label}</Text>
      </Pressable>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', gap: spacing.sm, marginBottom: spacing.md },
  btnWrap: { flex: 1 },
  btn: {
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  btnAtivoSafe: { backgroundColor: colors.safeDim, borderColor: colors.safeLine },
  btnAtivoWarn: { backgroundColor: colors.warnDim, borderColor: colors.warn },
  label: { color: colors.sub, fontSize: fontSize.meta, fontWeight: fontWeight.medium },
  labelAtivo: { color: colors.text },
});
