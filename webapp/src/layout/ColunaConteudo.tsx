import React from 'react';
import { StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { colors } from '../theme/theme';

/**
 * Web App — envolve uma tela inteira numa coluna centralizada com largura máxima.
 * Em telas estreitas (celular) a coluna ocupa 100% e nada muda em relação ao app
 * mobile; em telas largas, as laterais mostram só o fundo do app, e os cards
 * mantêm proporções próximas às do celular em vez de esticar.
 *
 * Base única (ADR 0039) — também é o único lugar que reserva o espaço da barra de status e da
 * barra de navegação do celular (o Android desenha o app de borda a borda). No navegador os
 * recuos são zero. Nas abas, quem reserva o espaço de baixo é a própria barra de abas
 * (`BottomNavigation`), por isso elas passam `recuoInferior={false}`.
 */
export function ColunaConteudo({
  larguraMaxima,
  recuoInferior = true,
  children,
}: {
  larguraMaxima: number;
  recuoInferior?: boolean;
  children: React.ReactNode;
}) {
  const insets = useSafeAreaInsets();
  return (
    <View style={[styles.fundo, { paddingTop: insets.top, paddingBottom: recuoInferior ? insets.bottom : 0 }]}>
      <View style={[styles.coluna, { maxWidth: larguraMaxima }]}>{children}</View>
    </View>
  );
}

const styles = StyleSheet.create({
  fundo: { flex: 1, backgroundColor: colors.bg, alignItems: 'center' },
  coluna: { flex: 1, width: '100%' },
});
