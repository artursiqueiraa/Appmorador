import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { ShieldCheck } from 'lucide-react-native';
import { PrimaryButton } from '../components/PrimaryButton';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

interface Props {
  onGuiado: () => void;
  onRapido: () => void;
}

/**
 * Web App — primeira tela de quem ainda não tem propriedade. Antes, o formulário
 * completo e o "Prefiro ser guiado" apareciam juntos, no rodapé, abaixo de uma área
 * vazia: dois caminhos competindo e nenhuma explicação. Agora o guiado é o caminho
 * principal, e o formulário rápido continua disponível como opção secundária.
 */
export function PrimeiraPropriedadeCard({ onGuiado, onRapido }: Props) {
  return (
    <View style={styles.cartao}>
      <View style={styles.iconeWrap}>
        <ShieldCheck size={iconSize.xl} color={colors.marca} />
      </View>
      <Text style={styles.titulo} accessibilityRole="header">
        Vamos configurar sua primeira propriedade
      </Text>
      <Text style={styles.descricao}>
        Em poucos passos você cadastra sua casa ou negócio. Dá para pular etapas e continuar depois, quando quiser.
      </Text>
      <View style={styles.acoes}>
        <PrimaryButton label="Começar configuração guiada" onPress={onGuiado} />
        <PrimaryButton label="Preencher tudo de uma vez" variant="secondary" onPress={onRapido} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  cartao: {
    alignItems: 'center',
    gap: spacing.sm,
    padding: spacing.xxl,
    marginTop: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.bg2,
    borderWidth: 1,
    borderColor: colors.line,
  },
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
  descricao: { color: colors.sub, fontSize: fontSize.body, textAlign: 'center' },
  acoes: { alignSelf: 'stretch', gap: spacing.sm, marginTop: spacing.lg },
});
