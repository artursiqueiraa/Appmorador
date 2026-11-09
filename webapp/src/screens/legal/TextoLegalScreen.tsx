import React from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import { ChevronLeft } from 'lucide-react-native';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';
import { POLITICA_PRIVACIDADE, TERMOS_USO, type DocumentoLegal } from './textosLegais';

/**
 * Sprint 26 (ADR 0042) — Termos de Uso e Política de Privacidade (fecha a dívida 55). Abre com ou
 * sem login. Dados da empresa marcados "[A PREENCHER]" até o usuário informar (decisão A).
 */
export function TextoLegalScreen() {
  const navigation = useNavigation();
  const { name } = useRoute<RouteProp<RootStackParamList, 'Termos' | 'Privacidade'>>();
  const documento: DocumentoLegal = name === 'Termos' ? TERMOS_USO : POLITICA_PRIVACIDADE;

  return (
    <ScrollView style={styles.tela} contentContainerStyle={styles.conteudo}>
      <View style={styles.cabecalho}>
        <Pressable onPress={() => navigation.goBack()} style={styles.voltar} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={{ flex: 1 }}>
          <Text style={styles.titulo}>{documento.titulo}</Text>
          <Text style={styles.versao}>Versão {documento.versao}</Text>
        </View>
      </View>
      {documento.secoes.map((s) => (
        <View key={s.titulo} style={styles.secao}>
          <Text style={styles.secaoTitulo}>{s.titulo}</Text>
          {s.paragrafos.map((p) => (
            <Text key={p.slice(0, 40)} style={styles.paragrafo}>
              {p}
            </Text>
          ))}
        </View>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tela: { flex: 1, backgroundColor: colors.bg },
  conteudo: { padding: spacing.xl, gap: spacing.lg, paddingBottom: spacing.xxxl },
  cabecalho: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
  voltar: {
    width: 44,
    height: 44,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  versao: { color: colors.mute, fontSize: fontSize.meta },
  secao: { gap: spacing.sm },
  secaoTitulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  paragrafo: { color: colors.sub, fontSize: fontSize.body, lineHeight: 21 },
});
