import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ChevronLeft, type LucideIcon } from 'lucide-react-native';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';
import { IconeQuadrado } from './IconeQuadrado';

interface Props {
  titulo: string;
  subtitulo?: string;
  /** Ícone opcional ao lado do título (ex.: Cadastro Facial). */
  icone?: LucideIcon;
  onVoltar?: () => void;
  direita?: React.ReactNode;
  /** Mantido por compatibilidade: o cabeçalho do protótipo não repete o logo em cada tela. */
  semMarca?: boolean;
}

/**
 * Akteck Seg (ADR 0038) — cabeçalho das telas no padrão do protótipo original: botão voltar
 * quadrado à esquerda, título e subtítulo alinhados à esquerda, ação opcional à direita. O logo
 * fica só na navegação (barra lateral / menu), não repetido no topo de cada tela.
 */
export function TelaCabecalho({ titulo, subtitulo, icone, onVoltar, direita }: Props) {
  return (
    <View style={styles.container}>
      {onVoltar ? (
        <Pressable onPress={onVoltar} style={styles.botaoQuadrado} accessibilityRole="button" accessibilityLabel="Voltar">
          <ChevronLeft size={iconSize.md} color={colors.text} />
        </Pressable>
      ) : null}
      {icone ? <IconeQuadrado icone={icone} tamanho={44} /> : null}
      <View style={styles.texto}>
        <Text style={styles.titulo} accessibilityRole="header">
          {titulo}
        </Text>
        {subtitulo ? <Text style={styles.subtitulo}>{subtitulo}</Text> : null}
      </View>
      {direita ? <View>{direita}</View> : null}
    </View>
  );
}

/** Botão quadrado com ícone (voltar, configurações, notificações). */
export function BotaoRedondo({ icone: Icone, onPress, rotulo, indicador }: { icone: LucideIcon; onPress: () => void; rotulo: string; indicador?: boolean }) {
  return (
    <Pressable onPress={onPress} style={styles.botaoQuadrado} accessibilityRole="button" accessibilityLabel={rotulo}>
      <Icone size={iconSize.md - 2} color={colors.sub} />
      {indicador ? <View style={styles.indicador} /> : null}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, paddingTop: spacing.sm, paddingBottom: spacing.lg },
  texto: { flex: 1, minWidth: 0 },
  botaoQuadrado: {
    width: 40,
    height: 40,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.line,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
  },
  indicador: { position: 'absolute', top: 9, right: 9, width: 8, height: 8, borderRadius: 4, backgroundColor: colors.danger, borderWidth: 2, borderColor: colors.surface },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  subtitulo: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 3 },
});
