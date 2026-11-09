import React from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { ChevronLeft, type LucideIcon } from 'lucide-react-native';
import { PrimaryButton } from '../../components/PrimaryButton';
import { useLayoutDesktop } from '../../layout/useLayoutDesktop';
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
  /** Sem `onVoltar`, o botão Voltar não aparece (ex.: logo depois de criar a propriedade, voltar recriaria ela). */
  onVoltar?: () => void;
}

/**
 * Sprint 16 (ADR 0019, UX001) — casca visual comum das 7 etapas do Onboarding (evita repetir progresso/rodapé em cada arquivo).
 *
 * Web App — "Etapa X de 7" + barra de progresso (antes eram só bolinhas sem rótulo),
 * botão Voltar, campos na largura toda (antes encolhiam para o tamanho do placeholder) e
 * ações logo abaixo do conteúdo. No desktop a etapa fica num cartão centralizado, em vez
 * de título no meio da tela e botão perdido no rodapé.
 */
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
  onVoltar,
}: Props) {
  const desktop = useLayoutDesktop();
  const numeroEtapa = etapaAtual + 1;
  const percentual = `${(numeroEtapa / totalEtapas) * 100}%` as const;

  return (
    <ScrollView style={styles.fundo} contentContainerStyle={styles.fundoConteudo} keyboardShouldPersistTaps="handled">
      <View style={[styles.cartao, desktop && styles.cartaoDesktop]}>
        <View style={styles.topo}>
          {onVoltar ? (
            <Pressable onPress={onVoltar} style={styles.voltar} accessibilityRole="button" accessibilityLabel="Voltar para a etapa anterior">
              <ChevronLeft size={iconSize.md} color={colors.sub} />
              <Text style={styles.voltarTexto}>Voltar</Text>
            </Pressable>
          ) : (
            <View />
          )}
          <Text style={styles.etapaTexto}>
            Etapa {numeroEtapa} de {totalEtapas}
          </Text>
        </View>

        <View
          style={styles.trilha}
          accessibilityRole="progressbar"
          accessibilityValue={{ min: 1, max: totalEtapas, now: numeroEtapa }}
        >
          <View style={[styles.preenchimento, { width: percentual }]} />
        </View>

        <View style={styles.cabecalho}>
          <View style={styles.iconeWrap}>
            <Icon size={iconSize.xl} color={colors.marca} />
          </View>
          <Text style={styles.titulo} accessibilityRole="header">
            {titulo}
          </Text>
          <Text style={styles.descricao}>{descricao}</Text>
        </View>

        {children ? <View style={styles.campos}>{children}</View> : null}

        <View style={styles.acoes}>
          {onAvancar ? (
            <PrimaryButton label={labelAvancar} onPress={onAvancar} disabled={avancarDesabilitado} loading={avancarCarregando} />
          ) : null}
          {onPular ? <PrimaryButton label="Pular por agora" variant="secondary" onPress={onPular} /> : null}
        </View>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  fundo: { flex: 1, backgroundColor: colors.bg },
  fundoConteudo: { flexGrow: 1, justifyContent: 'center', padding: spacing.xl },
  cartao: { width: '100%', gap: spacing.xl },
  cartaoDesktop: {
    padding: spacing.xxxl,
    borderRadius: radius.lg,
    backgroundColor: colors.bg2,
    borderWidth: 1,
    borderColor: colors.line,
  },
  topo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', minHeight: 44 },
  voltar: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    minHeight: 44,
    paddingRight: spacing.md,
  },
  voltarTexto: { color: colors.sub, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  etapaTexto: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  trilha: { height: 6, borderRadius: radius.pill, backgroundColor: colors.surface2, overflow: 'hidden', marginTop: -spacing.md },
  preenchimento: { height: '100%', borderRadius: radius.pill, backgroundColor: colors.marca },
  cabecalho: { alignItems: 'center', gap: spacing.sm },
  iconeWrap: {
    width: 72,
    height: 72,
    borderRadius: radius.pill,
    backgroundColor: colors.marcaDim,
    borderWidth: 1,
    borderColor: colors.marcaLine,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.sm,
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold, textAlign: 'center' },
  descricao: { color: colors.sub, fontSize: fontSize.body, textAlign: 'center', paddingHorizontal: spacing.md },
  campos: { width: '100%' },
  acoes: { gap: spacing.sm },
});
