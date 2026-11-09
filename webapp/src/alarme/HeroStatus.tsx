import React, { useEffect, useId, useState } from 'react';
import { Pressable, StyleSheet, Text, View, type LayoutChangeEvent } from 'react-native';
import Animated, { Easing, useAnimatedStyle, useReducedMotion, useSharedValue, withRepeat, withTiming } from 'react-native-reanimated';
import Svg, { Defs, RadialGradient, Rect, Stop } from 'react-native-svg';
import { Loader, ShieldAlert, ShieldCheck, ShieldOff, ShieldQuestion, Siren, type LucideIcon } from 'lucide-react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import type { EstadoAlarme } from './useCentralAlarme';

const VISUAL: Record<EstadoAlarme, { titulo: string; sub: string; cor: string; corDim: string; borda: string; Icone: LucideIcon; respira: boolean }> = {
  total: { titulo: 'PROTEGIDO', sub: 'Todas as áreas ativas', cor: colors.safe, corDim: colors.safeDim, borda: colors.safeLine, Icone: ShieldCheck, respira: true },
  parcial: { titulo: 'MODO NOTURNO', sub: 'Perímetro ativo, interior liberado', cor: colors.safe, corDim: colors.safeDim, borda: colors.safeLine, Icone: ShieldCheck, respira: true },
  desarmado: { titulo: 'DESARMADO', sub: 'O alarme está desligado no momento', cor: colors.warn, corDim: colors.warnDim, borda: colors.warnLine, Icone: ShieldOff, respira: false },
  disparo: { titulo: 'ALARME DISPARADO', sub: 'Verifique a propriedade agora', cor: colors.danger, corDim: colors.dangerDim, borda: colors.dangerLine, Icone: Siren, respira: true },
  semComunicacao: { titulo: 'SEM COMUNICAÇÃO', sub: 'A central não está respondendo agora', cor: colors.warn, corDim: colors.warnDim, borda: colors.warnLine, Icone: ShieldAlert, respira: false },
  semCentral: { titulo: 'CONFIGURE A PROTEÇÃO', sub: 'Adicione uma central para monitorar sua casa', cor: colors.sub, corDim: colors.surface2, borda: colors.line, Icone: ShieldQuestion, respira: false },
  carregando: { titulo: 'VERIFICANDO…', sub: 'Consultando a central', cor: colors.sub, corDim: colors.surface2, borda: colors.line, Icone: Loader, respira: false },
};

/** Anel que "respira" em volta do escudo enquanto a casa está protegida (desliga com movimento reduzido). */
function Anel({ cor, ativo }: { cor: string; ativo: boolean }) {
  const reduzir = useReducedMotion();
  const t = useSharedValue(0);
  useEffect(() => {
    t.value = ativo && !reduzir ? withRepeat(withTiming(1, { duration: 1700, easing: Easing.inOut(Easing.ease) }), -1, true) : 0;
  }, [ativo, reduzir, t]);
  const estilo = useAnimatedStyle(() => ({ transform: [{ scale: 1 + t.value * 0.14 }], opacity: 0.5 - t.value * 0.35 }));
  return <Animated.View style={[styles.anel, { borderColor: cor }, ativo ? estilo : styles.anelParado]} />;
}

/**
 * Brilho radial na cor do estado. Base única (ADR 0039): o tamanho vem do `onLayout`, em pixels —
 * no Android o SVG com "100%" era medido antes do cartão terminar o layout e não o cobria inteiro.
 */
function Brilho({ cor }: { cor: string }) {
  const id = `hero-${useId().replace(/:/g, '')}`;
  const [tamanho, setTamanho] = useState<{ largura: number; altura: number } | null>(null);
  const medir = (e: LayoutChangeEvent) => {
    const { width, height } = e.nativeEvent.layout;
    setTamanho((atual) => (atual?.largura === width && atual?.altura === height ? atual : { largura: width, altura: height }));
  };
  return (
    <View pointerEvents="none" style={StyleSheet.absoluteFill} onLayout={medir}>
      {tamanho ? (
        <Svg width={tamanho.largura} height={tamanho.altura}>
          <Defs>
            <RadialGradient id={id} cx="50%" cy="-10%" rx="80%" ry="90%">
              <Stop offset="0" stopColor={cor} stopOpacity={0.2} />
              <Stop offset="0.6" stopColor={colors.surface} stopOpacity={1} />
            </RadialGradient>
          </Defs>
          <Rect x="0" y="0" width={tamanho.largura} height={tamanho.altura} fill={`url(#${id})`} />
        </Svg>
      ) : null}
    </View>
  );
}

/**
 * Akteck Seg (ADR 0038) — cartão de status do protótipo original, a assinatura do app: brilho
 * radial na cor do estado, escudo com anel pulsando, título em caixa alta e uma linha de contexto.
 * Os estados vêm da central de verdade (`useCentralAlarme`).
 */
export function HeroStatus({ estado, detalhe, onPress }: { estado: EstadoAlarme; detalhe?: string | null; onPress?: () => void }) {
  const v = VISUAL[estado];
  return (
    <Pressable onPress={onPress} disabled={!onPress} style={[styles.cartao, { borderColor: v.borda }]} accessibilityRole={onPress ? 'button' : undefined} accessibilityLabel={`${v.titulo}. ${detalhe ?? v.sub}`}>
      <Brilho cor={v.cor} />
      <View style={styles.conteudo}>
        <View style={styles.escudoWrap}>
          <Anel cor={v.cor} ativo={v.respira} />
          <View style={[styles.escudo, { backgroundColor: v.corDim, borderColor: v.cor }]}>
            <v.Icone size={34} color={v.cor} />
          </View>
        </View>
        <Text style={styles.titulo}>{v.titulo}</Text>
        <Text style={styles.sub}>{detalhe ?? v.sub}</Text>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  cartao: { borderRadius: radius.xxl, borderWidth: 1, overflow: 'hidden', backgroundColor: colors.surface },
  conteudo: { alignItems: 'center', paddingTop: 26, paddingBottom: 24, paddingHorizontal: 22 },
  escudoWrap: { width: 74, height: 74, marginBottom: 14, alignItems: 'center', justifyContent: 'center' },
  anel: { position: 'absolute', width: 94, height: 94, borderRadius: 47, borderWidth: 1 },
  anelParado: { opacity: 0.3 },
  escudo: { width: 74, height: 74, borderRadius: 37, borderWidth: 1.5, alignItems: 'center', justifyContent: 'center' },
  titulo: { color: colors.text, fontSize: fontSize.hero, fontWeight: fontWeight.black, letterSpacing: 1, textAlign: 'center' },
  sub: { color: colors.sub, fontSize: fontSize.secondary, marginTop: spacing.xs + 2, textAlign: 'center' },
});
