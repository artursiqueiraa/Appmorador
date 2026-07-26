import React from 'react';
import { ScrollView, StyleSheet, Text } from 'react-native';
import { Video } from 'lucide-react-native';
import { EstadoVazio } from '../../components/EstadoVazio';
import { colors, fontSize, fontWeight, spacing } from '../../theme/theme';

/**
 * Sprint 16 (ADR 0019, UX001) — aba fixa da navegação inferior (Progressive
 * Disclosure/Navegação Previsível: a aba nunca fica escondida, mesmo sem
 * funcionalidade ainda). Hoje não existe nenhuma forma de cadastrar ou transmitir
 * uma câmera (sem API, sem CRUD — ver DIVIDA_TECNICA) — decisão confirmada com o
 * usuário: manter a aba com um Empty State honesto em vez de escondê-la ou fingir
 * câmeras reais.
 *
 * Sprint 17 (ADR 0020) — exceção deliberada à regra "todo Empty State tem CTA": sem
 * nenhuma ação real disponível (nem CRUD de câmera, nem fabricante de câmera com
 * suporte de verdade), um botão aqui só fingiria uma funcionalidade que não existe.
 * Mantido sem `cta`, consistente com a decisão acima.
 */
export function CamerasScreen() {
  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.titulo}>Câmeras</Text>
      <EstadoVazio
        icon={Video}
        titulo="Nenhuma câmera ainda"
        descricao="Esse recurso está chegando. Em breve você vai poder ver suas câmeras ao vivo direto por aqui."
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold, marginBottom: spacing.lg },
});
