import React, { useEffect, useRef, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Car, CircleCheck, DoorOpen, Fence, Lightbulb, Lock, ShieldQuestion, TriangleAlert } from 'lucide-react-native';
import type { IconePgm } from './pgmLabels';
import { registrarTelemetria } from '../services/telemetria';
import { PrimaryButton } from '../components/PrimaryButton';
import { CartaoNeon } from '../components/neon/CartaoNeon';
import { IconeQuadrado } from '../components/neon/IconeQuadrado';
import { colors, fontSize, fontWeight, spacing } from '../theme/theme';

const ICONES: Record<IconePgm, typeof DoorOpen> = {
  porta: DoorOpen,
  garagem: Car,
  luz: Lightbulb,
  fechadura: Lock,
  cancela: Fence,
  generico: ShieldQuestion,
};

/**
 * Sprint 18 (ADR 0022, Fase 6) — máquina de estados do comando. O backend
 * (`JflComandoServico.ExecutarComandoAsync`) é síncrono: o mesmo round-trip HTTP
 * já envia o comando E devolve o resultado — não existe um canal separado de
 * "confirmação assíncrona via SignalR" (verificado no código antes de desenhar
 * esta máquina, ver ADR 0022). Por isso "Aguardando Confirmação" e "Executando"
 * da missão original viram um único estado (`enviando`), resolvido pela mesma
 * resposta HTTP — nunca um estado fictício sem sinal real por trás.
 */
type EstadoComando = 'normal' | 'enviando' | 'sucesso' | 'falha';

const TIMEOUT_COMANDO_MS = 10000;
const DURACAO_SUCESSO_MS = 2000;

interface Props {
  icone: IconePgm;
  label: string;
  conectado: boolean;
  descricaoEstado: string;
  equipamentoId: string;
  /** Retorna se a ação teve sucesso — falha já é comunicada pelo chamador via Toast (com "Tentar novamente"). */
  onAcionar: () => Promise<boolean>;
}

/** Sprint 17 (ADR 0020) — um comando do Painel de Controle: ícone + nome amigável + estado + ação + feedback. */
export const CommandCard = React.memo(function CommandCard({ icone, label, conectado, descricaoEstado, equipamentoId, onAcionar }: Props) {
  const [estado, setEstado] = useState<EstadoComando>('normal');
  const [mensagemFalha, setMensagemFalha] = useState<string | null>(null);
  const inicioEnvioRef = useRef(0);
  const Icone = ICONES[icone];

  useEffect(() => {
    if (estado !== 'sucesso') {
      return;
    }
    const timeout = setTimeout(() => setEstado('normal'), DURACAO_SUCESSO_MS);
    return () => clearTimeout(timeout);
  }, [estado]);

  const acionar = async () => {
    setEstado('enviando');
    setMensagemFalha(null);
    inicioEnvioRef.current = Date.now();
    registrarTelemetria({ tipo: 'comando_enviado', comando: label, equipamentoId });

    let timeoutId: ReturnType<typeof setTimeout> | undefined;
    const timeoutPromise = new Promise<'timeout'>((resolve) => {
      timeoutId = setTimeout(() => resolve('timeout'), TIMEOUT_COMANDO_MS);
    });

    try {
      const resultado = await Promise.race([onAcionar(), timeoutPromise]);
      const msResposta = Date.now() - inicioEnvioRef.current;

      if (resultado === 'timeout') {
        registrarTelemetria({ tipo: 'comando_resultado', comando: label, sucesso: false, msResposta });
        setMensagemFalha('Não foi possível confirmar a execução. Verifique se o dispositivo está online.');
        setEstado('falha');
        return;
      }

      registrarTelemetria({ tipo: 'comando_resultado', comando: label, sucesso: resultado, msResposta });
      if (resultado) {
        setEstado('sucesso');
      } else {
        setMensagemFalha('Não foi possível concluir. Tente novamente.');
        setEstado('falha');
      }
    } catch {
      setMensagemFalha('Não foi possível concluir. Tente novamente.');
      setEstado('falha');
    } finally {
      if (timeoutId) {
        clearTimeout(timeoutId);
      }
    }
  };

  const desabilitado = !conectado || estado === 'enviando';
  const linhaEstado = estado === 'falha' ? mensagemFalha ?? descricaoEstado : estado === 'sucesso' ? 'Concluído ✓' : descricaoEstado;

  // Akteck Seg (ADR 0037) — cartão da referência "Portas e Acessos": ícone, nome, estado com ponto
  // colorido e o botão "Abrir" em degradê. "Manter aberta"/"Fechar" não aparecem: a central só
  // aceita o pulso de acionamento (PGM), e um botão sem comando real por trás seria enganoso.
  const corEstado = estado === 'falha' ? colors.warn : conectado ? colors.marca : colors.danger;
  return (
    <CartaoNeon style={styles.container}>
      <View style={styles.topo}>
        <IconeQuadrado icone={Icone} tamanho={48} tom={conectado ? 'marca' : 'neutro'} />
        <View style={styles.textoWrap}>
          <Text style={styles.label}>{label}</Text>
          <Text style={styles.sub}>Comando da central</Text>
        </View>
        <View style={styles.estadoPill}>
          <View style={[styles.ponto, { backgroundColor: corEstado }]} />
          <Text style={[styles.estado, { color: corEstado }]} numberOfLines={2}>
            {estado === 'sucesso' ? 'Concluído' : estado === 'falha' ? 'Falhou' : conectado ? 'Pronto' : 'Offline'}
          </Text>
        </View>
      </View>
      {estado === 'falha' || !conectado ? (
        <View style={styles.estadoLinha}>
          {estado === 'falha' ? <TriangleAlert size={12} color={colors.warn} /> : null}
          <Text style={[styles.detalhe, estado === 'falha' ? styles.estadoFalha : styles.estadoOffline]}>{linhaEstado}</Text>
        </View>
      ) : null}
      {estado === 'sucesso' ? (
        <View style={styles.estadoLinha}>
          <CircleCheck size={12} color={colors.marca} />
          <Text style={[styles.detalhe, styles.estadoOk]}>Comando executado.</Text>
        </View>
      ) : null}
      <PrimaryButton
        label={estado === 'falha' ? 'Tentar novamente' : icone === 'porta' || icone === 'garagem' || icone === 'cancela' ? 'Abrir' : label}
        icone={Icone}
        onPress={acionar}
        loading={estado === 'enviando'}
        disabled={desabilitado}
        compacto
      />
    </CartaoNeon>
  );
});

const styles = StyleSheet.create({
  container: { gap: spacing.md },
  topo: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
  textoWrap: { flex: 1, minWidth: 0 },
  label: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  sub: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  estadoPill: { flexDirection: 'row', alignItems: 'center', gap: 6, maxWidth: 120 },
  ponto: { width: 9, height: 9, borderRadius: 5 },
  estado: { fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  estadoLinha: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  detalhe: { fontSize: fontSize.secondary, flex: 1 },
  estadoOk: { color: colors.marca },
  estadoOffline: { color: colors.sub },
  estadoFalha: { color: colors.warn },
});
