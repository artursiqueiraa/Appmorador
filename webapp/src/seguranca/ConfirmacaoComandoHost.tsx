import React, { useEffect, useState } from 'react';
import { Modal, Pressable, StyleSheet, Text, View } from 'react-native';
import { Delete, KeyRound, ShieldCheck } from 'lucide-react-native';
import { PrimaryButton } from '../components/PrimaryButton';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import { registrarHostConfirmacao, type PedidoConfirmacao } from './confirmarComando';

const TECLAS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', 'apagar', '0', 'ok'] as const;

/**
 * Sprint 27 (ADR 0043) — folha de PIN e convite para criar o PIN, montada uma vez em App.tsx
 * (mesmo padrão do `DialogoAlertaHost`). Quem pede é `confirmarComando`.
 */
export function ConfirmacaoComandoHost() {
  const [pedido, setPedido] = useState<PedidoConfirmacao | null>(null);
  const [pin, setPin] = useState('');

  useEffect(() => {
    registrarHostConfirmacao((p) => {
      setPin('');
      setPedido(p);
    });
    return () => registrarHostConfirmacao(null);
  }, []);

  if (!pedido) {
    return null;
  }

  const fechar = () => setPedido(null);

  if (pedido.tipo === 'convite') {
    const responder = (escolha: 'criar' | 'continuar' | null) => {
      fechar();
      pedido.responder(escolha);
    };
    return (
      <Modal transparent visible animationType="fade" onRequestClose={() => responder(null)}>
        <View style={styles.fundo}>
          <View style={styles.caixa} accessibilityRole="alert">
            <View style={styles.icone}>
              <ShieldCheck size={26} color={colors.marca} />
            </View>
            <Text style={styles.titulo}>Proteja com um PIN</Text>
            <Text style={styles.texto}>
              Com um PIN, ninguém desarma o alarme ou abre um portão só por estar com o seu celular desbloqueado. Leva menos de um
              minuto.
            </Text>
            <PrimaryButton label="Criar PIN agora" icone={KeyRound} onPress={() => responder('criar')} />
            <PrimaryButton label="Continuar sem PIN" variant="secondary" onPress={() => responder('continuar')} />
            <Pressable onPress={() => responder(null)} style={styles.cancelar} accessibilityRole="button">
              <Text style={styles.cancelarTexto}>Cancelar</Text>
            </Pressable>
          </View>
        </View>
      </Modal>
    );
  }

  const responder = (valor: string | null) => {
    fechar();
    pedido.responder(valor);
  };

  const tocar = (tecla: (typeof TECLAS)[number]) => {
    if (tecla === 'apagar') {
      setPin((p) => p.slice(0, -1));
    } else if (tecla === 'ok') {
      if (pin.length >= 4) {
        responder(pin);
      }
    } else if (pin.length < 6) {
      setPin((p) => p + tecla);
    }
  };

  return (
    <Modal transparent visible animationType="fade" onRequestClose={() => responder(null)}>
      <View style={styles.fundo}>
        <View style={styles.caixa}>
          <Text style={styles.titulo}>Confirme com seu PIN</Text>
          <Text style={styles.texto}>{pedido.acao}</Text>

          <View style={styles.pontos} accessibilityLabel={`${pin.length} de até 6 números digitados`}>
            {Array.from({ length: Math.max(4, Math.min(6, pin.length + 1)) }).map((_, i) => (
              <View key={i} style={[styles.ponto, i < pin.length && styles.pontoCheio]} />
            ))}
          </View>

          {pedido.erro ? (
            <Text style={styles.erro} accessibilityRole="alert">
              {pedido.erro}
            </Text>
          ) : null}

          <View style={styles.teclado}>
            {TECLAS.map((tecla) => {
              const desabilitada = tecla === 'ok' && pin.length < 4;
              return (
                <Pressable
                  key={tecla}
                  onPress={() => tocar(tecla)}
                  disabled={desabilitada}
                  accessibilityRole="button"
                  accessibilityLabel={tecla === 'apagar' ? 'Apagar' : tecla === 'ok' ? 'Confirmar' : tecla}
                  style={({ pressed }) => [
                    styles.tecla,
                    tecla === 'ok' && styles.teclaOk,
                    desabilitada && styles.teclaDesabilitada,
                    pressed && styles.teclaPressionada,
                  ]}
                >
                  {tecla === 'apagar' ? (
                    <Delete size={22} color={colors.text} />
                  ) : (
                    <Text style={[styles.teclaTexto, tecla === 'ok' && styles.teclaOkTexto]}>{tecla === 'ok' ? 'OK' : tecla}</Text>
                  )}
                </Pressable>
              );
            })}
          </View>

          <Pressable onPress={() => responder(null)} style={styles.cancelar} accessibilityRole="button">
            <Text style={styles.cancelarTexto}>Cancelar</Text>
          </Pressable>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  fundo: { flex: 1, backgroundColor: colors.overlay, alignItems: 'center', justifyContent: 'center', padding: spacing.lg },
  caixa: {
    width: '100%',
    maxWidth: 360,
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.line,
    padding: spacing.xl,
    gap: spacing.md,
  },
  icone: {
    width: 52,
    height: 52,
    borderRadius: radius.md,
    backgroundColor: colors.marcaDim,
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold, textAlign: 'center' },
  texto: { color: colors.sub, fontSize: fontSize.secondary, textAlign: 'center' },
  pontos: { flexDirection: 'row', justifyContent: 'center', gap: spacing.md, marginVertical: spacing.sm },
  ponto: { width: 14, height: 14, borderRadius: 7, borderWidth: 2, borderColor: colors.sub },
  pontoCheio: { backgroundColor: colors.marca, borderColor: colors.marca },
  erro: { color: colors.danger, fontSize: fontSize.secondary, textAlign: 'center' },
  teclado: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between', rowGap: spacing.sm },
  tecla: {
    width: '31%',
    height: 56,
    borderRadius: radius.md,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  teclaOk: { backgroundColor: colors.marca },
  teclaDesabilitada: { opacity: 0.4 },
  teclaPressionada: { opacity: 0.7 },
  teclaTexto: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold },
  teclaOkTexto: { color: colors.onMarca, fontSize: fontSize.section },
  cancelar: { alignSelf: 'center', paddingVertical: spacing.sm, paddingHorizontal: spacing.lg, minHeight: 44, justifyContent: 'center' },
  cancelarTexto: { color: colors.sub, fontSize: fontSize.body },
});
