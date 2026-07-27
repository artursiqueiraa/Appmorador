import React, { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { Car, CircleCheck, DoorOpen, Fence, Lightbulb, Lock, ShieldQuestion, TriangleAlert } from 'lucide-react-native';
import type { IconePgm } from './pgmLabels';
import { registrarTelemetria } from '../services/telemetria';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

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

  return (
    <View style={styles.container}>
      <View style={styles.iconWrap}>
        <Icone size={iconSize.md} color={conectado ? colors.accent : colors.mute} />
      </View>
      <View style={styles.textoWrap}>
        <Text style={styles.label}>{label}</Text>
        <View style={styles.estadoLinha}>
          {estado === 'falha' ? <TriangleAlert size={12} color={colors.warn} /> : null}
          {estado === 'sucesso' ? <CircleCheck size={12} color={colors.safe} /> : null}
          <Text
            style={[
              styles.estado,
              estado === 'falha' ? styles.estadoFalha : conectado ? styles.estadoOk : styles.estadoOffline,
            ]}
          >
            {linhaEstado}
          </Text>
        </View>
      </View>
      <Pressable
        onPress={acionar}
        disabled={desabilitado}
        accessibilityLabel={conectado ? label : `${label} — dispositivo offline`}
        style={[styles.botao, !conectado && styles.botaoDesabilitado, estado === 'falha' && styles.botaoFalha]}
      >
        {estado === 'enviando' ? (
          <ActivityIndicator color={colors.safe} size="small" />
        ) : (
          <Text style={[styles.botaoLabel, estado === 'falha' && styles.botaoLabelFalha]}>
            {estado === 'falha' ? 'Tentar novamente' : label}
          </Text>
        )}
      </Pressable>
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  iconWrap: {
    width: 42,
    height: 42,
    borderRadius: radius.md,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  textoWrap: { flex: 1, minWidth: 0 },
  label: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  estadoLinha: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 2 },
  estado: { fontSize: fontSize.tiny },
  estadoOk: { color: colors.safe },
  estadoOffline: { color: colors.mute },
  estadoFalha: { color: colors.warn },
  botao: {
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderRadius: radius.md,
    backgroundColor: colors.safeDim,
    borderWidth: 1,
    borderColor: colors.safeLine,
    minWidth: 96,
    alignItems: 'center',
  },
  botaoDesabilitado: { backgroundColor: colors.surface2, borderColor: colors.line, opacity: 0.6 },
  botaoFalha: { backgroundColor: colors.warnDim, borderColor: colors.warnLine },
  botaoLabel: { color: colors.safe, fontSize: fontSize.meta, fontWeight: fontWeight.bold },
  botaoLabelFalha: { color: colors.warn },
});
