import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import type { TipoUnidade } from '../api/types';

const OPCOES: { valor: TipoUnidade; rotulo: string }[] = [
  { valor: 'Casa', rotulo: 'Casa' },
  { valor: 'Apartamento', rotulo: 'Apartamento' },
  { valor: 'Loja', rotulo: 'Loja' },
  { valor: 'SalaComercial', rotulo: 'Sala Comercial' },
  { valor: 'Galpao', rotulo: 'Galpão' },
  { valor: 'Quiosque', rotulo: 'Quiosque' },
  { valor: 'Escritorio', rotulo: 'Escritório' },
  { valor: 'Outro', rotulo: 'Outro' },
];

/** Reaproveitado por telas que só precisam exibir o rótulo (ex.: badge no card da unidade). */
export function rotuloTipoUnidade(tipo: TipoUnidade): string {
  return OPCOES.find((o) => o.valor === tipo)?.rotulo ?? tipo;
}

interface Props {
  label: string;
  value: TipoUnidade | null;
  onChange: (valor: TipoUnidade) => void;
}

/** Mesmo padrão visual de `TipoPropriedadeSelector` — chips de seleção única. */
export function TipoUnidadeSelector({ label, value, onChange }: Props) {
  return (
    <View style={styles.container}>
      <Text style={styles.label}>{label}</Text>
      <View style={styles.chipsRow}>
        {OPCOES.map((opcao) => {
          const ativo = opcao.valor === value;
          return (
            <Pressable
              key={opcao.valor}
              onPress={() => onChange(opcao.valor)}
              style={[styles.chip, ativo && styles.chipAtivo]}
            >
              <Text style={[styles.chipLabel, ativo && styles.chipLabelAtivo]}>{opcao.rotulo}</Text>
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginBottom: spacing.lg,
  },
  label: {
    color: colors.sub,
    fontSize: fontSize.meta,
    fontWeight: fontWeight.medium,
    marginBottom: spacing.xs + 2,
  },
  chipsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
  },
  chip: {
    paddingVertical: spacing.xs + 2,
    paddingHorizontal: spacing.md,
    borderRadius: radius.pill,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  chipAtivo: {
    backgroundColor: colors.safeDim,
    borderColor: colors.safeLine,
  },
  chipLabel: {
    color: colors.sub,
    fontSize: fontSize.secondary,
    fontWeight: fontWeight.medium,
  },
  chipLabelAtivo: {
    color: colors.safe,
  },
});
