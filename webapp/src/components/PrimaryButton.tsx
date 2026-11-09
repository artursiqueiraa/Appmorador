import React from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import type { LucideIcon } from 'lucide-react-native';
import { colors, efeitos, fontSize, fontWeight, iconSize, opacity, radius, spacing } from '../theme/theme';
import { Degrade } from './neon/Degrade';

interface Props {
  label: string;
  onPress: () => void;
  loading?: boolean;
  disabled?: boolean;
  /** `primary`: degradê verde com brilho. `secondary`: contorno verde sobre fundo escuro. `danger`: contorno vermelho. */
  variant?: 'primary' | 'secondary' | 'danger';
  icone?: LucideIcon;
  compacto?: boolean;
}

/**
 * Akteck Seg (ADR 0037) — botão da identidade oficial: principal em degradê verde neon com
 * brilho discreto; secundário com contorno verde (como "Manter aberta" / "Criar conta" na
 * referência). Mesma API de antes, então todas as telas existentes herdam o visual novo.
 */
export function PrimaryButton({ label, onPress, loading, disabled, variant = 'primary', icone: Icone, compacto }: Props) {
  const isPrimary = variant === 'primary';
  const corTexto = isPrimary ? colors.onMarca : variant === 'danger' ? colors.danger : colors.text;
  const corIcone = isPrimary ? colors.onMarca : variant === 'danger' ? colors.danger : colors.marca;

  return (
    <Pressable
      onPress={onPress}
      disabled={disabled || loading}
      accessibilityRole="button"
      accessibilityState={{ disabled: !!(disabled || loading), busy: !!loading }}
      style={({ pressed }) => [
        styles.base,
        compacto && styles.compacto,
        isPrimary ? styles.primary : variant === 'danger' ? styles.danger : styles.secondary,
        (disabled || loading) && styles.disabled,
        pressed && styles.pressed,
      ]}
    >
      {isPrimary ? <Degrade cores={efeitos.degradeMarca} /> : null}
      {loading ? (
        <ActivityIndicator color={corTexto} />
      ) : (
        <View style={styles.conteudo}>
          {Icone ? <Icone size={iconSize.md} color={corIcone} /> : null}
          <Text style={[styles.label, compacto && styles.labelCompacto, { color: corTexto }]}>{label}</Text>
        </View>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  base: {
    minHeight: 52,
    borderRadius: radius.lg,
    paddingHorizontal: spacing.lg,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 10,
    overflow: 'hidden',
  },
  compacto: { minHeight: 44, marginTop: 0, paddingHorizontal: spacing.md, borderRadius: radius.md },
  primary: { boxShadow: efeitos.brilhoMarca },
  secondary: { backgroundColor: 'transparent', borderWidth: 1, borderColor: colors.marcaLine },
  danger: { backgroundColor: 'transparent', borderWidth: 1, borderColor: colors.dangerLine },
  disabled: { opacity: opacity.disabled },
  pressed: { opacity: opacity.pressed },
  conteudo: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
  label: { fontSize: fontSize.body + 2, fontWeight: fontWeight.black },
  labelCompacto: { fontSize: fontSize.body },
});
