import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import type { LucideIcon } from 'lucide-react-native';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';
import { PrimaryButton } from './PrimaryButton';

interface Props {
  icon: LucideIcon;
  titulo: string;
  descricao: string;
  /** Sprint 16 (ADR 0019, UX001) — toda lista vazia responde "o que devo fazer agora?" com uma ação, nunca só "0"/"nenhum item". */
  cta?: { label: string; onPress: () => void };
}

/**
 * Componente "EmptyState" do Design System UX001 — nome mantido em pt-BR
 * (consistente com o resto do domínio do projeto) desde a Sprint 4. Copy sempre
 * tranquilizadora e acionável — nunca linguagem de sistema tipo "0 itens
 * encontrados".
 */
export function EstadoVazio({ icon: Icon, titulo, descricao, cta }: Props) {
  return (
    <View style={styles.container}>
      <View style={styles.iconWrap}>
        <Icon size={iconSize.xl} color={colors.accent} />
      </View>
      <Text style={styles.titulo}>{titulo}</Text>
      <Text style={styles.descricao}>{descricao}</Text>
      {cta ? (
        <View style={styles.ctaWrap}>
          <PrimaryButton label={cta.label} onPress={cta.onPress} />
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    padding: spacing.xxl,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.lineSoft,
    marginBottom: spacing.md,
  },
  iconWrap: {
    width: 64,
    height: 64,
    borderRadius: 999,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.md,
  },
  titulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold, textAlign: 'center' },
  descricao: {
    color: colors.sub,
    fontSize: fontSize.secondary,
    textAlign: 'center',
    marginTop: spacing.xs,
  },
  ctaWrap: { width: '100%', marginTop: spacing.md },
});
