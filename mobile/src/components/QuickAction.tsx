import React from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';
import type { LucideIcon } from 'lucide-react-native';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

interface Props {
  icon: LucideIcon;
  label: string;
  active?: boolean;
  /** Ação "desliga proteção" usa a cor de atenção quando ativa, nunca a de sucesso — o desarme nunca deve parecer o estado "bom". */
  tone?: 'safe' | 'warn';
  onPress: () => void;
  disabled?: boolean;
}

/**
 * Sprint 16 (ADR 0019, UX001) — botão de ação rápida com ícone e label (HeroCard, telas de detalhe).
 * Sprint 18 (ADR 0022, Regra 5) — memoizado: não deve re-renderizar quando o
 * HeroCard atualiza por um snapshot que não muda `active`/`disabled` (exige que o
 * chamador passe `onPress` estável via `useCallback`, ver HomeScreen).
 */
export const QuickAction = React.memo(function QuickAction({ icon: Icon, label, active, tone = 'safe', onPress, disabled }: Props) {
  const corAtiva = tone === 'warn' ? colors.warn : colors.safe;
  const bgAtivo = tone === 'warn' ? colors.warnDim : colors.safeDim;
  const borderAtivo = tone === 'warn' ? colors.warnLine : colors.safeLine;

  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      style={({ pressed }) => [
        styles.container,
        {
          backgroundColor: active ? bgAtivo : colors.surface,
          borderColor: active ? borderAtivo : colors.line,
          opacity: disabled ? 0.4 : pressed ? 0.85 : 1,
        },
      ]}
    >
      <Icon size={iconSize.md} color={active ? corAtiva : colors.sub} />
      <Text style={[styles.label, { color: active ? colors.text : colors.sub }]}>{label}</Text>
    </Pressable>
  );
});

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.lg,
    paddingHorizontal: spacing.xs,
    borderRadius: radius.lg,
    borderWidth: 1,
  },
  label: { fontSize: fontSize.meta, fontWeight: fontWeight.medium },
});
