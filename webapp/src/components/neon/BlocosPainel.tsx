import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import type { LucideIcon } from 'lucide-react-native';
import { colors, efeitos, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';
import type { TomEvento } from '../../utils/iconeEvento';
import { IconeQuadrado } from './IconeQuadrado';

/** Akteck Seg (ADR 0037) — número em destaque com ícone e rótulo ("Câmeras 12"). Clicável quando leva a uma tela. */
export function NumeroDestaque({ icone: Icone, rotulo, valor, tom = 'marca', onPress }: { icone: LucideIcon; rotulo: string; valor: number; tom?: 'marca' | 'perigo'; onPress?: () => void }) {
  const cor = tom === 'perigo' ? colors.danger : colors.marca;
  return (
    <Pressable onPress={onPress} disabled={!onPress} style={styles.numero} accessibilityRole={onPress ? 'button' : undefined} accessibilityLabel={`${rotulo}: ${valor}`}>
      <Icone size={iconSize.lg} color={cor} />
      <Text style={styles.numeroRotulo} numberOfLines={1}>
        {rotulo}
      </Text>
      <Text style={styles.numeroValor}>{valor.toLocaleString('pt-BR')}</Text>
    </Pressable>
  );
}

/** Akteck Seg (ADR 0037) — atalho de "Ações rápidas" (Abrir Porta, Ver Câmeras...). */
export function AcaoRapida({ icone: Icone, rotulo, onPress }: { icone: LucideIcon; rotulo: string; onPress: () => void }) {
  return (
    <Pressable onPress={onPress} style={({ pressed }) => [styles.acao, pressed && styles.acaoPressionada]} accessibilityRole="button" accessibilityLabel={rotulo.replace('\n', ' ')}>
      <Icone size={iconSize.lg} color={colors.marca} />
      <Text style={styles.acaoRotulo}>{rotulo}</Text>
    </Pressable>
  );
}

/** Akteck Seg (ADR 0037) — linha de evento: ícone colorido pelo tipo, título, detalhe e horário. */
export function LinhaEvento({ icone, tom, titulo, detalhe, hora, onPress }: { icone: LucideIcon; tom: TomEvento; titulo: string; detalhe?: string | null; hora: string; onPress?: () => void }) {
  return (
    <Pressable onPress={onPress} disabled={!onPress} style={styles.evento} accessibilityRole={onPress ? 'button' : undefined}>
      <IconeQuadrado icone={icone} tom={tom} tamanho={42} />
      <View style={styles.eventoTexto}>
        <Text style={styles.eventoTitulo} numberOfLines={1}>
          {titulo}
        </Text>
        {detalhe ? (
          <Text style={styles.eventoDetalhe} numberOfLines={1}>
            {detalhe}
          </Text>
        ) : null}
      </View>
      <Text style={styles.eventoHora}>{hora}</Text>
    </Pressable>
  );
}

/** Título de seção com link à direita ("Últimos eventos — Ver todos →"). */
export function TituloSecao({ titulo, acao, onAcao }: { titulo: string; acao?: string; onAcao?: () => void }) {
  return (
    <View style={styles.secao}>
      <Text style={styles.secaoTitulo}>{titulo}</Text>
      {acao && onAcao ? (
        <Pressable onPress={onAcao} accessibilityRole="button" style={styles.secaoAcao}>
          <Text style={styles.secaoAcaoTexto}>{acao} →</Text>
        </Pressable>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  numero: {
    flex: 1,
    minWidth: 0,
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.xs,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.marcaLine,
    backgroundColor: colors.surface,
    boxShadow: efeitos.brilhoSuave,
  },
  numeroRotulo: { color: colors.sub, fontSize: fontSize.tiny, fontWeight: fontWeight.medium },
  numeroValor: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  acao: {
    flex: 1,
    minWidth: 0,
    minHeight: 96,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    padding: spacing.sm,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.marcaLine,
    backgroundColor: colors.surface,
    boxShadow: efeitos.brilhoSuave,
  },
  acaoPressionada: { backgroundColor: colors.marcaDim },
  acaoRotulo: { color: colors.text, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, textAlign: 'center' },
  evento: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, paddingVertical: spacing.md, borderBottomWidth: 1, borderBottomColor: colors.lineSoft },
  eventoTexto: { flex: 1, minWidth: 0 },
  eventoTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  eventoDetalhe: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  eventoHora: { color: colors.sub, fontSize: fontSize.secondary },
  secao: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: spacing.xl, marginBottom: spacing.md },
  secaoTitulo: { color: colors.text, fontSize: fontSize.section + 2, fontWeight: fontWeight.black },
  secaoAcao: { minHeight: 40, justifyContent: 'center' },
  secaoAcaoTexto: { color: colors.marca, fontSize: fontSize.secondary, fontWeight: fontWeight.bold },
});
