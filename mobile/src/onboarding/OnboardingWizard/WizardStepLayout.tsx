import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import type { LucideIcon } from 'lucide-react-native';
import { PrimaryButton } from '../../components/PrimaryButton';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

interface Props {
  icon: LucideIcon;
  titulo: string;
  descricao: string;
  etapaAtual: number;
  totalEtapas: number;
  children?: React.ReactNode;
  onAvancar?: () => void;
  labelAvancar?: string;
  avancarDesabilitado?: boolean;
  avancarCarregando?: boolean;
  onPular?: () => void;
}

/** Sprint 16 (ADR 0019, UX001) — casca visual comum das 7 etapas do Onboarding (evita repetir progresso/rodapé em cada arquivo). */
export function WizardStepLayout({
  icon: Icon,
  titulo,
  descricao,
  etapaAtual,
  totalEtapas,
  children,
  onAvancar,
  labelAvancar = 'Continuar',
  avancarDesabilitado,
  avancarCarregando,
  onPular,
}: Props) {
  return (
    <View style={styles.container}>
      <View style={styles.progresso}>
        {Array.from({ length: totalEtapas }).map((_, i) => (
          <View key={i} style={[styles.ponto, i <= etapaAtual && styles.pontoAtivo]} />
        ))}
      </View>

      <View style={styles.conteudo}>
        <View style={styles.iconeWrap}>
          <Icon size={iconSize.xl} color={colors.safe} />
        </View>
        <Text style={styles.titulo}>{titulo}</Text>
        <Text style={styles.descricao}>{descricao}</Text>
        {children}
      </View>

      <View style={styles.rodape}>
        {onAvancar ? (
          <PrimaryButton label={labelAvancar} onPress={onAvancar} disabled={avancarDesabilitado} loading={avancarCarregando} />
        ) : null}
        {onPular ? <PrimaryButton label="Pular por agora" variant="secondary" onPress={onPular} /> : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: spacing.xl, justifyContent: 'space-between' },
  progresso: { flexDirection: 'row', gap: spacing.xs, justifyContent: 'center', marginBottom: spacing.xl },
  ponto: { width: 8, height: 8, borderRadius: radius.pill, backgroundColor: colors.surface2 },
  pontoAtivo: { backgroundColor: colors.safe },
  conteudo: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: spacing.sm },
  iconeWrap: {
    width: 72,
    height: 72,
    borderRadius: 999,
    backgroundColor: colors.safeDim,
    borderWidth: 1,
    borderColor: colors.safeLine,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.md,
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold, textAlign: 'center' },
  descricao: { color: colors.sub, fontSize: fontSize.body, textAlign: 'center', marginTop: spacing.xs, paddingHorizontal: spacing.md },
  rodape: { gap: spacing.sm },
});
