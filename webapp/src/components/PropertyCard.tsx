import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ChevronRight, Home } from 'lucide-react-native';
import { rotuloTipoPropriedade, type TipoPropriedade } from './TipoPropriedadeSelector';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

interface Props {
  nome: string;
  tipo: TipoPropriedade;
  endereco?: string | null;
  onPress: () => void;
}

/** Sprint 16 (ADR 0019, UX001) — card de resumo da propriedade (Início, Ajustes → Minha Propriedade). */
export function PropertyCard({ nome, tipo, endereco, onPress }: Props) {
  return (
    <Pressable onPress={onPress} style={styles.container}>
      <View style={styles.iconWrap}>
        <Home size={iconSize.md} color={colors.accent} />
      </View>
      <View style={styles.textWrap}>
        <Text style={styles.nome}>{nome}</Text>
        <Text style={styles.detalhe}>
          {rotuloTipoPropriedade(tipo)}
          {endereco ? ` · ${endereco}` : ''}
        </Text>
      </View>
      <ChevronRight size={16} color={colors.mute} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  iconWrap: {
    width: 42,
    height: 42,
    borderRadius: radius.md,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  textWrap: { flex: 1, minWidth: 0 },
  nome: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.bold },
  detalhe: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
});
