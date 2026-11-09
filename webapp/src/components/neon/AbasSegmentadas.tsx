import React from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { colors, efeitos, fontSize, fontWeight, radius, spacing } from '../../theme/theme';
import { Degrade } from './Degrade';

export interface Aba<T extends string> {
  valor: T;
  rotulo: string;
  contagem?: number;
}

/**
 * Akteck Seg (ADR 0037) — abas em pílula ("Portas (8) | Catracas (2)", "Todos | Alarmes | Acessos").
 * A ativa fica em degradê verde. Rola na horizontal quando não cabe (celular).
 */
export function AbasSegmentadas<T extends string>({ abas, valor, onChange }: { abas: Aba<T>[]; valor: T; onChange: (v: T) => void }) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.linha}>
      <View style={styles.trilho} accessibilityRole="tablist">
        {abas.map((aba) => {
          const ativa = aba.valor === valor;
          return (
            <Pressable
              key={aba.valor}
              onPress={() => onChange(aba.valor)}
              style={[styles.aba, ativa && styles.abaAtiva]}
              accessibilityRole="tab"
              accessibilityState={{ selected: ativa }}
            >
              {ativa ? <Degrade cores={efeitos.degradeMarca} /> : null}
              <Text style={[styles.texto, ativa && styles.textoAtivo]}>
                {aba.rotulo}
                {aba.contagem !== undefined ? ` (${aba.contagem})` : ''}
              </Text>
            </Pressable>
          );
        })}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  linha: { flexGrow: 1 },
  trilho: {
    flexDirection: 'row',
    flexGrow: 1,
    gap: spacing.xs,
    padding: spacing.xs,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  aba: { flexGrow: 1, minHeight: 40, paddingHorizontal: spacing.lg, borderRadius: radius.md, alignItems: 'center', justifyContent: 'center', overflow: 'hidden' },
  abaAtiva: { boxShadow: efeitos.brilhoMarca },
  texto: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  textoAtivo: { color: colors.onMarca, fontWeight: fontWeight.bold },
});
