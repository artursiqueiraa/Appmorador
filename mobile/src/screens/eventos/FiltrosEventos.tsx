import React from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Search } from 'lucide-react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

export type PeriodoFiltro = 'hoje' | '7dias' | '30dias' | 'tudo';

const OPCOES_PERIODO: { valor: PeriodoFiltro; rotulo: string }[] = [
  { valor: 'hoje', rotulo: 'Hoje' },
  { valor: '7dias', rotulo: '7 dias' },
  { valor: '30dias', rotulo: '30 dias' },
  { valor: 'tudo', rotulo: 'Tudo' },
];

/** Traduz um filtro de período pré-definido num intervalo UTC — nunca expõe um date-picker. */
export function periodoParaIntervaloUtc(periodo: PeriodoFiltro): { desdeUtc?: string } {
  if (periodo === 'tudo') {
    return {};
  }

  const dias = periodo === 'hoje' ? 1 : periodo === '7dias' ? 7 : 30;
  const desde = new Date(Date.now() - dias * 24 * 60 * 60 * 1000);
  return { desdeUtc: desde.toISOString() };
}

interface Props {
  periodo: PeriodoFiltro;
  onChangePeriodo: (periodo: PeriodoFiltro) => void;
  busca: string;
  onChangeBusca: (busca: string) => void;
}

export function FiltrosEventos({ periodo, onChangePeriodo, busca, onChangeBusca }: Props) {
  return (
    <View style={styles.container}>
      <View style={styles.buscaWrap}>
        <Search size={16} color={colors.mute} />
        <TextInput
          value={busca}
          onChangeText={onChangeBusca}
          placeholder="Buscar por zona ou evento"
          placeholderTextColor={colors.mute}
          style={styles.buscaInput}
          accessibilityLabel="Buscar eventos por zona ou descrição"
        />
      </View>
      <View style={styles.chipsRow}>
        {OPCOES_PERIODO.map((opcao) => {
          const ativo = opcao.valor === periodo;
          return (
            <Pressable
              key={opcao.valor}
              onPress={() => onChangePeriodo(opcao.valor)}
              style={[styles.chip, ativo && styles.chipAtivo]}
              accessibilityRole="button"
              accessibilityState={{ selected: ativo }}
              accessibilityLabel={`Período: ${opcao.rotulo}`}
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
  container: { marginBottom: spacing.md },
  buscaWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    borderRadius: radius.md,
    paddingHorizontal: spacing.md,
    marginBottom: spacing.sm,
  },
  buscaInput: {
    color: colors.text,
    fontSize: fontSize.body,
    paddingVertical: 10,
  },
  chipsRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs },
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