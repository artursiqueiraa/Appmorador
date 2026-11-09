import React from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { Lock, MoonStar, Unlock, type LucideIcon } from 'lucide-react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import type { ComandoAlarme, EstadoAlarme } from './useCentralAlarme';

const ACOES: { comando: ComandoAlarme; rotulo: string; Icone: LucideIcon; ativoEm: EstadoAlarme; alerta?: boolean }[] = [
  { comando: 'armar', rotulo: 'Armar total', Icone: Lock, ativoEm: 'total' },
  { comando: 'armar-stay', rotulo: 'Noturno', Icone: MoonStar, ativoEm: 'parcial' },
  { comando: 'desarmar', rotulo: 'Desarmar', Icone: Unlock, ativoEm: 'desarmado', alerta: true },
];

/**
 * Akteck Seg (ADR 0038) — os três botões do protótipo, agora com comando real na central
 * ("Noturno" = arme parcial `armar-stay`). O botão do estado atual fica aceso; todos ficam
 * desabilitados enquanto a central não responde.
 */
export function BotoesArme({ estado, enviando, disponivel, onComando }: { estado: EstadoAlarme; enviando: ComandoAlarme | null; disponivel: boolean; onComando: (c: ComandoAlarme) => void }) {
  return (
    <View style={styles.grade}>
      {ACOES.map((a) => {
        const ligado = estado === a.ativoEm;
        const corAtiva = a.alerta ? colors.warn : colors.safe;
        const desabilitado = !disponivel || enviando !== null;
        return (
          <Pressable
            key={a.comando}
            onPress={() => onComando(a.comando)}
            disabled={desabilitado}
            accessibilityRole="button"
            accessibilityState={{ selected: ligado, disabled: desabilitado, busy: enviando === a.comando }}
            style={(st) => [
              styles.botao,
              ligado && { backgroundColor: a.alerta ? colors.warnDim : colors.safeDim, borderColor: a.alerta ? colors.warnLine : colors.safeLine },
              (st as { hovered?: boolean }).hovered && !desabilitado && !ligado && styles.botaoHover,
              desabilitado && !ligado && styles.botaoDesabilitado,
            ]}
          >
            {enviando === a.comando ? <ActivityIndicator color={corAtiva} /> : <a.Icone size={20} color={ligado ? corAtiva : colors.sub} />}
            <Text style={[styles.rotulo, ligado && styles.rotuloLigado]}>{a.rotulo}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  grade: { flexDirection: 'row', gap: 10 },
  botao: {
    flex: 1,
    alignItems: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.lg,
    paddingHorizontal: 6,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  botaoHover: { backgroundColor: colors.surface2 },
  botaoDesabilitado: { opacity: 0.55 },
  rotulo: { color: colors.sub, fontSize: fontSize.meta, fontWeight: fontWeight.medium },
  rotuloLigado: { color: colors.text },
});
