import React from 'react';
import { StyleSheet, View } from 'react-native';
import { Search } from 'lucide-react-native';
import { spacing } from '../../theme/theme';
import { TextField } from '../../components/TextField';
import { AbasSegmentadas } from '../../components/neon/AbasSegmentadas';

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

/** Akteck Seg (ADR 0037) — busca com ícone e período em abas segmentadas (visual da referência). */
export function FiltrosEventos({ periodo, onChangePeriodo, busca, onChangeBusca }: Props) {
  return (
    <View style={styles.container}>
      <AbasSegmentadas abas={OPCOES_PERIODO} valor={periodo} onChange={onChangePeriodo} />
      <TextField label="Buscar" icone={Search} value={busca} onChangeText={onChangeBusca} placeholder="Buscar por zona ou evento" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { gap: spacing.md, marginBottom: spacing.sm },
});
