import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import type { FabricanteEquipamento } from '../api/types';

const OPCOES: { valor: FabricanteEquipamento; rotulo: string }[] = [
  { valor: 'ControlId', rotulo: 'Control iD' },
  { valor: 'Jfl', rotulo: 'JFL' },
  { valor: 'Intelbras', rotulo: 'Intelbras' },
  { valor: 'Hikvision', rotulo: 'Hikvision' },
  { valor: 'Dahua', rotulo: 'Dahua' },
  { valor: 'Outro', rotulo: 'Outro' },
];

/** Reaproveitado por telas que só precisam exibir o rótulo (ex.: card/detalhes do equipamento). */
export function rotuloFabricanteEquipamento(fabricante: FabricanteEquipamento): string {
  return OPCOES.find((o) => o.valor === fabricante)?.rotulo ?? fabricante;
}

interface Props {
  label: string;
  value: FabricanteEquipamento | null;
  onChange: (valor: FabricanteEquipamento) => void;
}

/** Mesmo padrão visual de `TipoEntregaSelector` — chips de seleção única. */
export function FabricanteEquipamentoSelector({ label, value, onChange }: Props) {
  return (
    <View style={styles.container}>
      <Text style={styles.label}>{label}</Text>
      <View style={styles.chipsRow}>
        {OPCOES.map((opcao) => {
          const ativo = opcao.valor === value;
          return (
            <Pressable key={opcao.valor} onPress={() => onChange(opcao.valor)} style={[styles.chip, ativo && styles.chipAtivo]}>
              <Text style={[styles.chipLabel, ativo && styles.chipLabelAtivo]}>{opcao.rotulo}</Text>
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { marginBottom: spacing.lg },
  label: { color: colors.sub, fontSize: fontSize.meta, fontWeight: fontWeight.medium, marginBottom: spacing.xs + 2 },
  chipsRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs },
  chip: {
    paddingVertical: spacing.xs + 2,
    paddingHorizontal: spacing.md,
    borderRadius: radius.pill,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  chipAtivo: { backgroundColor: colors.safeDim, borderColor: colors.safeLine },
  chipLabel: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  chipLabelAtivo: { color: colors.safe },
});
