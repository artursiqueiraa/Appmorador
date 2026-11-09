import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

interface Props<T extends string> {
  rotulo: string;
  opcoes: { valor: T; rotulo: string }[];
  valor: T | null | undefined;
  onChange: (valor: T) => void;
  dica?: string;
}

/**
 * Akteck Seg (ADR 0036) — escolha única entre poucas opções, em "chips". Genérico de propósito:
 * os `Tipo*Selector` antigos repetem este mesmo desenho, cada um com sua lista fixa.
 */
export function OpcoesChips<T extends string>({ rotulo, opcoes, valor, onChange, dica }: Props<T>) {
  return (
    <View style={styles.container}>
      <Text style={styles.rotulo}>{rotulo}</Text>
      <View style={styles.linha} accessibilityRole="radiogroup" accessibilityLabel={rotulo}>
        {opcoes.map((opcao) => {
          const ativo = opcao.valor === valor;
          return (
            <Pressable
              key={opcao.valor}
              onPress={() => onChange(opcao.valor)}
              style={[styles.chip, ativo && styles.chipAtivo]}
              accessibilityRole="radio"
              accessibilityState={{ checked: ativo }}
            >
              <Text style={[styles.chipTexto, ativo && styles.chipTextoAtivo]}>{opcao.rotulo}</Text>
            </Pressable>
          );
        })}
      </View>
      {dica ? <Text style={styles.dica}>{dica}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { marginBottom: spacing.lg },
  rotulo: { color: colors.sub, fontSize: fontSize.meta, fontWeight: fontWeight.medium, marginBottom: spacing.sm },
  linha: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm },
  chip: {
    minHeight: 40,
    justifyContent: 'center',
    paddingHorizontal: spacing.lg,
    borderRadius: radius.pill,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.borderStrong,
  },
  chipAtivo: { backgroundColor: colors.marcaDim, borderColor: colors.marca },
  chipTexto: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  chipTextoAtivo: { color: colors.text },
  dica: { color: colors.mute, fontSize: fontSize.tiny, marginTop: spacing.sm },
});
