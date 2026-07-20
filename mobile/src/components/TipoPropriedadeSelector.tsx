import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

export type TipoPropriedade = 'Residencial' | 'Comercial' | 'Condominio' | 'Rural' | 'Outro';

const OPCOES: { valor: TipoPropriedade; rotulo: string; descricao: string }[] = [
  { valor: 'Residencial', rotulo: 'Residencial', descricao: 'Sua casa ou apartamento' },
  { valor: 'Comercial', rotulo: 'Comercial', descricao: 'Lojas, escritórios, clínicas e pequenos negócios' },
  { valor: 'Condominio', rotulo: 'Condomínio', descricao: 'Áreas comuns de um condomínio' },
  { valor: 'Rural', rotulo: 'Rural', descricao: 'Sítios, chácaras e fazendas' },
  { valor: 'Outro', rotulo: 'Outro', descricao: 'Não se encaixa nas opções acima' },
];

/** Reaproveitado por telas que só precisam exibir o rótulo (ex.: badge no card da propriedade). */
export function rotuloTipoPropriedade(tipo: TipoPropriedade): string {
  return OPCOES.find((o) => o.valor === tipo)?.rotulo ?? tipo;
}

interface Props {
  label: string;
  value: TipoPropriedade | null;
  onChange: (valor: TipoPropriedade) => void;
}

export function TipoPropriedadeSelector({ label, value, onChange }: Props) {
  const selecionada = OPCOES.find((o) => o.valor === value);

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
      {selecionada ? <Text style={styles.descricao}>{selecionada.descricao}</Text> : null}
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
  descricao: {
    color: colors.mute,
    fontSize: fontSize.tiny,
    marginTop: spacing.xs + 2,
  },
});
