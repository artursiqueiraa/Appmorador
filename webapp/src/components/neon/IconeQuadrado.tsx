import React from 'react';
import { StyleSheet, View } from 'react-native';
import type { LucideIcon } from 'lucide-react-native';
import { colors, radius } from '../../theme/theme';

type Tom = 'marca' | 'perigo' | 'atencao' | 'neutro';

const TONS: Record<Tom, { cor: string; fundo: string; borda: string }> = {
  marca: { cor: colors.marca, fundo: colors.marcaDim, borda: colors.marcaLine },
  perigo: { cor: colors.danger, fundo: colors.dangerDim, borda: colors.dangerLine },
  atencao: { cor: colors.warn, fundo: colors.warnDim, borda: colors.warnLine },
  neutro: { cor: colors.sub, fundo: colors.surface2, borda: colors.line },
};

/** Akteck Seg (ADR 0037) — ícone dentro de um quadrado arredondado com contorno verde (listas, cabeçalhos, eventos). */
export function IconeQuadrado({ icone: Icone, tamanho = 44, tom = 'marca' }: { icone: LucideIcon; tamanho?: number; tom?: Tom }) {
  const t = TONS[tom];
  return (
    <View style={[styles.base, { width: tamanho, height: tamanho, backgroundColor: t.fundo, borderColor: t.borda }]}>
      <Icone size={Math.round(tamanho * 0.48)} color={t.cor} />
    </View>
  );
}

const styles = StyleSheet.create({
  base: { borderRadius: radius.md, borderWidth: 1, alignItems: 'center', justifyContent: 'center' },
});
