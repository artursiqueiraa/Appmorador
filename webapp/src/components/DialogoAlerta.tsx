import React, { useEffect, useState } from 'react';
import { Modal, Pressable, StyleSheet, Text, View, type AlertButton } from 'react-native';
import { colors, fontSize, fontWeight, layout, opacity, radius, spacing } from '../theme/theme';

/**
 * Web App — o `Alert.alert` do react-native-web é uma implementação vazia: no
 * navegador nenhuma confirmação apareceria (sair da conta, excluir, acionar
 * comando...) e a ação nunca rodaria. Este módulo expõe a MESMA assinatura
 * (`Alert.alert(titulo, mensagem, botoes)`), então as telas só trocam o import, e
 * renderiza um diálogo com os tokens do Design System.
 */
interface Pedido {
  titulo: string;
  mensagem?: string;
  botoes: AlertButton[];
}

let mostrar: ((pedido: Pedido) => void) | null = null;

export const Alert = {
  alert(titulo: string, mensagem?: string, botoes?: AlertButton[]): void {
    const pedido = { titulo, mensagem, botoes: botoes?.length ? botoes : [{ text: 'OK' }] };
    if (mostrar) {
      mostrar(pedido);
    } else if (typeof window !== 'undefined') {
      // Fallback só se o host ainda não montou — nunca deixa a mensagem sumir.
      window.alert(mensagem ? `${titulo}\n\n${mensagem}` : titulo);
    }
  },
};

/** Montado uma única vez em `App.tsx`, acima da navegação. */
export function DialogoAlertaHost() {
  const [pedido, setPedido] = useState<Pedido | null>(null);

  useEffect(() => {
    mostrar = setPedido;
    return () => {
      mostrar = null;
    };
  }, []);

  if (!pedido) {
    return null;
  }

  const fechar = () => setPedido(null);
  const botaoCancelar = pedido.botoes.find((b) => b.style === 'cancel');

  const escolher = (botao: AlertButton) => {
    fechar();
    botao.onPress?.();
  };

  return (
    <Modal transparent visible animationType="fade" onRequestClose={() => (botaoCancelar ? escolher(botaoCancelar) : fechar())}>
      <View style={styles.fundo}>
        <View style={styles.caixa} accessibilityRole="alert">
          <Text style={styles.titulo}>{pedido.titulo}</Text>
          {pedido.mensagem ? <Text style={styles.mensagem}>{pedido.mensagem}</Text> : null}
          <View style={styles.botoes}>
            {pedido.botoes.map((botao, indice) => {
              const destrutivo = botao.style === 'destructive';
              const secundario = botao.style === 'cancel';
              return (
                <Pressable
                  key={`${botao.text}-${indice}`}
                  onPress={() => escolher(botao)}
                  accessibilityRole="button"
                  style={({ pressed }) => [
                    styles.botao,
                    secundario ? styles.botaoSecundario : destrutivo ? styles.botaoDestrutivo : styles.botaoPrimario,
                    pressed && styles.botaoPressionado,
                  ]}
                >
                  <Text
                    style={[
                      styles.botaoTexto,
                      secundario ? styles.botaoTextoSecundario : destrutivo ? styles.botaoTextoDestrutivo : styles.botaoTextoPrimario,
                    ]}
                  >
                    {botao.text ?? 'OK'}
                  </Text>
                </Pressable>
              );
            })}
          </View>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  fundo: {
    flex: 1,
    backgroundColor: colors.overlay,
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.xl,
  },
  caixa: {
    width: '100%',
    maxWidth: layout.formMaxWidth,
    padding: spacing.xl,
    gap: spacing.sm,
    borderRadius: radius.lg,
    backgroundColor: colors.bg2,
    borderWidth: 1,
    borderColor: colors.line,
  },
  titulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  mensagem: { color: colors.sub, fontSize: fontSize.body },
  botoes: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'flex-end', gap: spacing.sm, marginTop: spacing.md },
  botao: { minHeight: 44, paddingHorizontal: spacing.lg, borderRadius: radius.sm, alignItems: 'center', justifyContent: 'center' },
  botaoPrimario: { backgroundColor: colors.safe },
  botaoSecundario: { backgroundColor: colors.surface2, borderWidth: 1, borderColor: colors.line },
  botaoDestrutivo: { backgroundColor: colors.dangerDim, borderWidth: 1, borderColor: colors.dangerLine },
  botaoPressionado: { opacity: opacity.pressed },
  botaoTexto: { fontSize: fontSize.body, fontWeight: fontWeight.bold },
  botaoTextoPrimario: { color: colors.bg },
  botaoTextoSecundario: { color: colors.text },
  botaoTextoDestrutivo: { color: colors.danger },
});
