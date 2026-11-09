import React, { useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, type TextInputProps, View } from 'react-native';
import { Eye, EyeOff, type LucideIcon } from 'lucide-react-native';
import { colors, efeitos, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

interface Props extends TextInputProps {
  label: string;
  error?: string;
  /** Akteck Seg (ADR 0037) — ícone à esquerda do campo, como na referência (e-mail, senha, telefone). */
  icone?: LucideIcon;
}

/**
 * Campo padrão. Akteck Seg (ADR 0037): fundo escuro, borda verde (≥ 3:1), brilho quando em foco,
 * ícone opcional e botão de mostrar/ocultar nos campos de senha.
 */
export function TextField({ label, error, style, icone: Icone, secureTextEntry, onFocus, onBlur, ...rest }: Props) {
  const [focado, setFocado] = useState(false);
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const ehSenha = !!secureTextEntry;

  return (
    <View style={styles.container}>
      <Text style={styles.label}>{label}</Text>
      <View style={[styles.campo, focado && styles.campoFocado, error ? styles.campoErro : null]}>
        {Icone ? <Icone size={iconSize.md} color={focado ? colors.marca : colors.sub} /> : null}
        <TextInput
          placeholderTextColor={colors.mute}
          style={[styles.input, style]}
          secureTextEntry={ehSenha && !mostrarSenha}
          onFocus={(e) => {
            setFocado(true);
            onFocus?.(e);
          }}
          onBlur={(e) => {
            setFocado(false);
            onBlur?.(e);
          }}
          accessibilityLabel={label}
          {...rest}
        />
        {ehSenha ? (
          <Pressable
            onPress={() => setMostrarSenha((v) => !v)}
            style={styles.olho}
            accessibilityRole="button"
            accessibilityLabel={mostrarSenha ? 'Ocultar senha' : 'Mostrar senha'}
          >
            {mostrarSenha ? <EyeOff size={iconSize.md} color={colors.sub} /> : <Eye size={iconSize.md} color={colors.sub} />}
          </Pressable>
        ) : null}
      </View>
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { marginBottom: spacing.lg },
  label: { color: colors.text, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, marginBottom: spacing.sm },
  campo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 54,
    paddingHorizontal: spacing.lg,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    backgroundColor: colors.surface,
  },
  campoFocado: { borderColor: colors.marca, boxShadow: efeitos.brilhoMarca },
  campoErro: { borderColor: colors.dangerLine },
  input: { flex: 1, minWidth: 0, paddingVertical: 14, color: colors.text, fontSize: fontSize.body + 1, outlineStyle: 'none' } as object,
  olho: { width: 40, height: 40, alignItems: 'center', justifyContent: 'center' },
  error: { color: colors.danger, fontSize: fontSize.tiny, marginTop: spacing.xs },
});
