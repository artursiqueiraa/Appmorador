import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Check } from 'lucide-react-native';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

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
  /** Web App — `cartoes` mostra cada opção com a descrição visível (usado no Onboarding); `chips` é o visual compacto original. */
  variante?: 'chips' | 'cartoes';
}

export function TipoPropriedadeSelector({ label, value, onChange, variante = 'chips' }: Props) {
  const selecionada = OPCOES.find((o) => o.valor === value);

  if (variante === 'cartoes') {
    return (
      <View style={styles.container} accessibilityRole="radiogroup" accessibilityLabel={label}>
        <View style={styles.cartoes}>
          {OPCOES.map((opcao) => {
            const ativo = opcao.valor === value;
            return (
              <Pressable
                key={opcao.valor}
                onPress={() => onChange(opcao.valor)}
                style={[styles.cartao, ativo && styles.cartaoAtivo]}
                accessibilityRole="radio"
                accessibilityState={{ checked: ativo }}
                accessibilityLabel={`${opcao.rotulo}: ${opcao.descricao}`}
              >
                <View style={styles.cartaoTexto}>
                  <Text style={[styles.cartaoRotulo, ativo && styles.chipLabelAtivo]}>{opcao.rotulo}</Text>
                  <Text style={styles.cartaoDescricao}>{opcao.descricao}</Text>
                </View>
                <View style={[styles.marcador, ativo && styles.marcadorAtivo]}>
                  {ativo ? <Check size={iconSize.sm} color={colors.bg} /> : null}
                </View>
              </Pressable>
            );
          })}
        </View>
      </View>
    );
  }

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
    backgroundColor: colors.marcaDim,
    borderColor: colors.marcaLine,
  },
  chipLabel: {
    color: colors.sub,
    fontSize: fontSize.secondary,
    fontWeight: fontWeight.medium,
  },
  chipLabelAtivo: {
    color: colors.marca,
  },
  cartoes: { gap: spacing.sm },
  cartao: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 56,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.lg,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  cartaoAtivo: { backgroundColor: colors.marcaDim, borderColor: colors.marcaLine },
  cartaoTexto: { flex: 1, minWidth: 0, gap: 2 },
  cartaoRotulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  cartaoDescricao: { color: colors.sub, fontSize: fontSize.secondary },
  marcador: {
    width: 22,
    height: 22,
    borderRadius: radius.pill,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  marcadorAtivo: { backgroundColor: colors.marca, borderColor: colors.marca },
  descricao: {
    color: colors.mute,
    fontSize: fontSize.tiny,
    marginTop: spacing.xs + 2,
  },
});
