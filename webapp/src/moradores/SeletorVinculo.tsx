import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Check, House, UserRound, Users, type LucideIcon } from 'lucide-react-native';
import type { TipoMorador } from '../api/types';
import { colors, efeitos, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

export type Vinculo = TipoMorador | 'Visitante';

/**
 * Akteck Seg (ADR 0037) — "Tipo de vínculo" da referência, só com o que o sistema tem de verdade:
 * Morador (titular), Dependente e Visitante (este segue para o cadastro de visitantes, que já
 * existe). Lojista e Prestador não existem no domínio, então não aparecem.
 */
const OPCOES: { valor: Vinculo; titulo: string; sub: string; icone: LucideIcon }[] = [
  { valor: 'Titular', titulo: 'Morador', sub: '(Titular)', icone: House },
  { valor: 'Dependente', titulo: 'Dependente', sub: '(Família)', icone: Users },
  { valor: 'Visitante', titulo: 'Visitante', sub: '(Eventual)', icone: UserRound },
];

export function SeletorVinculo({ valor, onChange, semVisitante }: { valor: Vinculo; onChange: (v: Vinculo) => void; semVisitante?: boolean }) {
  return (
    <View style={styles.linha} accessibilityRole="radiogroup" accessibilityLabel="Tipo de vínculo">
      {OPCOES.filter((o) => !(semVisitante && o.valor === 'Visitante')).map((o) => {
        const ativo = o.valor === valor;
        return (
          <Pressable
            key={o.valor}
            onPress={() => onChange(o.valor)}
            style={[styles.cartao, ativo && styles.cartaoAtivo]}
            accessibilityRole="radio"
            accessibilityState={{ checked: ativo }}
            accessibilityLabel={`${o.titulo} ${o.sub}`}
          >
            {ativo ? (
              <View style={styles.selo}>
                <Check size={12} color={colors.onMarca} strokeWidth={3} />
              </View>
            ) : null}
            <o.icone size={iconSize.lg} color={ativo ? colors.marca : colors.sub} />
            <Text style={[styles.titulo, ativo && styles.tituloAtivo]}>{o.titulo}</Text>
            <Text style={styles.sub}>{o.sub}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  linha: { flexDirection: 'row', gap: spacing.sm },
  cartao: {
    flex: 1,
    minWidth: 0,
    alignItems: 'center',
    gap: 4,
    paddingVertical: spacing.lg,
    paddingHorizontal: spacing.xs,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.line,
    backgroundColor: colors.surface,
  },
  cartaoAtivo: { borderColor: colors.marca, backgroundColor: colors.marcaDim, boxShadow: efeitos.brilhoAtivo },
  selo: {
    position: 'absolute',
    top: 6,
    right: 6,
    width: 20,
    height: 20,
    borderRadius: 10,
    backgroundColor: colors.marca,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold, marginTop: spacing.xs },
  tituloAtivo: { color: colors.marca },
  sub: { color: colors.sub, fontSize: fontSize.tiny },
});
