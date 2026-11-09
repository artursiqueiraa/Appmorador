import React, { useId } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import Svg, { Defs, LinearGradient, Path, Stop } from 'react-native-svg';
import { colors, efeitos, fontWeight } from '../../theme/theme';

/**
 * Identidade Akteck Seg (ADR 0037) — símbolo da referência oficial: um "A" verde em degradê
 * atravessado por uma órbita. Proporção 4:3 (64×48).
 */
export function SimboloAkteck({ tamanho = 40 }: { tamanho?: number }) {
  const id = `akteck-${useId().replace(/:/g, '')}`;
  return (
    <Svg width={tamanho} height={tamanho * 0.75} viewBox="0 0 64 48">
      <Defs>
        <LinearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <Stop offset="0" stopColor={efeitos.degradeMarca[0]} />
          <Stop offset="1" stopColor={efeitos.degradeMarca[1]} />
        </LinearGradient>
      </Defs>
      <Path d="M5 46 27 3h9l22 43H45L31.5 18 18 46Z" fill={`url(#${id})`} />
      <Path d="M2 34c14-9 38-15 60-12-3-3-9-4-14-4C30 18 12 25 2 34Z" fill={colors.marca} />
    </Svg>
  );
}

/**
 * `empilhada`: símbolo em cima de "AKTECK SEG" e "SEGURANÇA INTELIGENTE" (login, cabeçalhos).
 * `linha`: símbolo ao lado de "AKTECK SEG" (barra lateral).
 */
export function MarcaAkteck({ tamanho = 28, variante = 'linha' }: { tamanho?: number; variante?: 'linha' | 'empilhada' }) {
  const empilhada = variante === 'empilhada';
  return (
    <View style={empilhada ? styles.coluna : styles.linha} accessible accessibilityRole="image" accessibilityLabel="Akteck Seg, segurança inteligente">
      <SimboloAkteck tamanho={empilhada ? tamanho * 1.9 : tamanho * 1.15} />
      <View style={empilhada ? styles.textoCentro : undefined}>
        <Text style={[styles.nome, { fontSize: tamanho * (empilhada ? 0.95 : 0.66) }]}>
          AKTECK <Text style={styles.seg}>SEG</Text>
        </Text>
        {empilhada ? <Text style={[styles.lema, { fontSize: tamanho * 0.3 }]}>SEGURANÇA INTELIGENTE</Text> : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  linha: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  coluna: { alignItems: 'center', gap: 4 },
  textoCentro: { alignItems: 'center' },
  nome: { color: colors.text, fontWeight: fontWeight.black, fontStyle: 'italic', letterSpacing: 0.5 },
  seg: { color: colors.marca },
  lema: { color: colors.sub, fontWeight: fontWeight.medium, letterSpacing: 3, marginTop: 2 },
});
