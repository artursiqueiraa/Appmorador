import React, { useId, useState } from 'react';
import { StyleSheet, View, type LayoutChangeEvent } from 'react-native';
import Svg, { Defs, LinearGradient, Rect, Stop } from 'react-native-svg';

/**
 * Akteck Seg (ADR 0037) — degradê de fundo em SVG (funciona igual no navegador e no celular,
 * sem dependência nova). Preenche o pai, que deve ter `overflow: 'hidden'` e o mesmo raio.
 * No navegador o degradê fica por cima de irmãos não posicionados (ícones SVG soltos): envolva o
 * conteúdo em uma `View` (que já é posicionada) para ele aparecer na frente.
 *
 * Base única (ADR 0039) — no Android o SVG com largura "100%" era medido antes do pai terminar o
 * layout e ficava mais curto que o botão. Agora o tamanho vem do `onLayout`, em pixels. Com as
 * duas cores iguais não há degradê de verdade: vira um fundo sólido, sem SVG.
 */
export function Degrade({ cores, horizontal = false, opacidade = 1 }: { cores: readonly [string, string]; horizontal?: boolean; opacidade?: number }) {
  const id = `degrade-${useId().replace(/:/g, '')}`;
  const [tamanho, setTamanho] = useState<{ largura: number; altura: number } | null>(null);

  if (cores[0] === cores[1]) {
    return <View pointerEvents="none" style={[StyleSheet.absoluteFill, { backgroundColor: cores[0], opacity: opacidade }]} />;
  }

  const medir = (e: LayoutChangeEvent) => {
    const { width, height } = e.nativeEvent.layout;
    setTamanho((atual) => (atual?.largura === width && atual?.altura === height ? atual : { largura: width, altura: height }));
  };

  return (
    <View pointerEvents="none" style={StyleSheet.absoluteFill} onLayout={medir}>
      {tamanho ? (
        <Svg width={tamanho.largura} height={tamanho.altura}>
          <Defs>
            <LinearGradient id={id} x1="0" y1="0" x2={horizontal ? '1' : '0'} y2={horizontal ? '0' : '1'}>
              <Stop offset="0" stopColor={cores[0]} stopOpacity={opacidade} />
              <Stop offset="1" stopColor={cores[1]} stopOpacity={opacidade} />
            </LinearGradient>
          </Defs>
          <Rect x="0" y="0" width={tamanho.largura} height={tamanho.altura} fill={`url(#${id})`} />
        </Svg>
      ) : null}
    </View>
  );
}
