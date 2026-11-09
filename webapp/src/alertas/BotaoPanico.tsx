import React, { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { Siren } from 'lucide-react-native';
import { ServicoFeedbackTatil } from '../services/ServicoFeedbackTatil';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import { alertaAtivoStore } from './alertaAtivoStore';
import { servicoAlertas } from './servicoAlertas';
import { useSegurarParaAcionar } from './useSegurarParaAcionar';

export const DURACAO_PANICO_MS = 3000;

/**
 * Sprint 24 (ADR 0040) — botão de pânico do Início. Precisa ficar pressionado por 3 segundos
 * (a barra enche enquanto segura); soltar antes cancela. Ao completar, abre o alerta crítico
 * para o dono e as pessoas da lista, registrando quem acionou.
 */
export function BotaoPanico({ propriedadeId }: { propriedadeId: string }) {
  const [enviando, setEnviando] = useState(false);
  const [mensagem, setMensagem] = useState<string | null>(null);

  const acionar = async () => {
    ServicoFeedbackTatil.notificationError();
    setEnviando(true);
    setMensagem(null);
    try {
      const alerta = await servicoAlertas.acionarPanico(propriedadeId);
      alertaAtivoStore.abrirExplicitamente(alerta);
    } catch {
      setMensagem('Não foi possível enviar o pedido de socorro. Ligue 190.');
    } finally {
      setEnviando(false);
    }
  };

  const { segurando, progresso, iniciar, cancelar } = useSegurarParaAcionar(DURACAO_PANICO_MS, acionar);
  const segundosRestantes = Math.max(1, Math.ceil(((1 - progresso) * DURACAO_PANICO_MS) / 1000));

  return (
    <View>
      <Pressable
        onPressIn={() => {
          ServicoFeedbackTatil.impactMedium();
          iniciar();
        }}
        onPressOut={cancelar}
        disabled={enviando}
        style={[styles.botao, segurando && styles.botaoSegurando]}
        accessibilityRole="button"
        accessibilityLabel="Pânico. Mantenha pressionado por 3 segundos para pedir socorro"
      >
        <View style={[styles.progresso, { width: `${progresso * 100}%` }]} />
        <View style={styles.conteudo}>
          {enviando ? <ActivityIndicator color={colors.danger} /> : <Siren size={18} color={colors.danger} />}
          <Text style={styles.rotulo}>
            {enviando ? 'Enviando pedido de socorro…' : segurando ? `Continue segurando… ${segundosRestantes}` : 'Pânico — segure por 3 segundos'}
          </Text>
        </View>
      </Pressable>
      {mensagem ? <Text style={styles.mensagem}>{mensagem}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  botao: {
    minHeight: 52,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.dangerLine,
    backgroundColor: colors.surface,
    overflow: 'hidden',
    justifyContent: 'center',
  },
  botaoSegurando: { borderColor: colors.danger },
  progresso: { position: 'absolute', top: 0, bottom: 0, left: 0, backgroundColor: colors.dangerDim },
  conteudo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: spacing.sm, paddingHorizontal: spacing.lg },
  rotulo: { color: colors.danger, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  mensagem: { color: colors.danger, fontSize: fontSize.secondary, marginTop: spacing.xs, textAlign: 'center' },
});
