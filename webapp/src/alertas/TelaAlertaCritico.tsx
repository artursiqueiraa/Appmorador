import React, { useEffect, useState } from 'react';
import { AppState, Linking, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import Animated, { FadeIn, useAnimatedStyle, useSharedValue, withRepeat, withTiming } from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Camera, CheckCircle2, Phone, Siren, X } from 'lucide-react-native';
import { useAuth } from '../auth/AuthContext';
import { useRealtimeConexao } from '../realtime/RealtimeContext';
import { iniciarAvisoAlerta, pararAvisoAlerta } from './avisoSonoro';
import { navigationRef } from '../navigation/navigationRef';
import { PrimaryButton } from '../components/PrimaryButton';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import type { AlertaCriticoResponse, ContatoEmergenciaResponse } from '../api/types';
import { alertaAtivoStore, useAlertaAtivo } from './alertaAtivoStore';
import { formatarTelefone, servicoAlertas } from './servicoAlertas';
import { situacaoEscalonamento } from './textosAlerta';

/**
 * Sprint 24 (ADR 0040) — alerta crítico em tela cheia, por cima de qualquer tela. Substitui o
 * antigo `AlertaDisparo` de demonstração (Sprint 16): agora é sempre um alerta real do servidor.
 * "Estou ciente" confirma para todos e encerra o escalonamento; o X só esconde neste aparelho.
 * Busca alertas ainda abertos ao entrar numa propriedade, ao voltar para o app (tela desbloqueada,
 * troca de app) e quando o tempo real reconecta: com o app em segundo plano a conexão cai, e um
 * alerta aberto nesse intervalo não pode se perder.
 */
export function TelaAlertaCritico() {
  const alerta = useAlertaAtivo();
  const { selectedProperty } = useAuth();
  const propriedadeId = selectedProperty?.id ?? null;
  const { estadoConexao } = useRealtimeConexao();

  useEffect(() => {
    if (!propriedadeId) {
      return;
    }
    let cancelado = false;
    const verificar = () =>
      servicoAlertas
        .listarAbertos(propriedadeId)
        .then((abertos) => {
          if (!cancelado && abertos.length > 0) {
            alertaAtivoStore.receber(abertos[0]);
          }
        })
        .catch(() => {
          // Sem conexão agora: a próxima volta ao app ou reconexão verifica de novo.
        });

    if (estadoConexao !== 'conectando' && estadoConexao !== 'reconectando') {
      verificar();
    }
    // Rede de segurança: sem tempo real, consulta a cada 15 s — um alerta crítico nunca espera
    // a conexão voltar para aparecer.
    const consulta = estadoConexao === 'conectado' ? null : setInterval(verificar, 15000);
    const assinatura = AppState.addEventListener('change', (estado) => {
      if (estado === 'active') {
        verificar();
      }
    });
    return () => {
      cancelado = true;
      if (consulta) {
        clearInterval(consulta);
      }
      assinatura.remove();
    };
  }, [propriedadeId, estadoConexao]);

  if (!alerta) {
    return null;
  }

  return <ConteudoAlerta key={alerta.id} alerta={alerta} nomePropriedade={selectedProperty?.nome ?? ''} />;
}

function ConteudoAlerta({ alerta, nomePropriedade }: { alerta: AlertaCriticoResponse; nomePropriedade: string }) {
  const insets = useSafeAreaInsets();
  const [contatos, setContatos] = useState<ContatoEmergenciaResponse[]>([]);
  const [confirmando, setConfirmando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const pulso = useSharedValue(1);

  useEffect(() => {
    pulso.value = withRepeat(withTiming(0.35, { duration: 550 }), -1, true);
  }, [pulso]);

  // Som de alarme e vibração enquanto esta tela estiver aberta.
  useEffect(() => {
    iniciarAvisoAlerta(alerta);
    return () => pararAvisoAlerta(alerta.id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [alerta.id]);

  useEffect(() => {
    // Só o dono vê a lista; para membros a chamada falha e os botões de contato não aparecem.
    servicoAlertas.listarContatos(alerta.propriedadeId).then(setContatos).catch(() => setContatos([]));
  }, [alerta.propriedadeId]);

  const estiloPulso = useAnimatedStyle(() => ({ opacity: pulso.value }));
  const hora = new Date(alerta.criadoEmUtc).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });

  const confirmar = async () => {
    setConfirmando(true);
    setErro(null);
    try {
      const confirmado = await servicoAlertas.confirmar(alerta.id);
      alertaAtivoStore.receber(confirmado);
    } catch {
      setErro('Não foi possível confirmar agora. Verifique a internet e tente de novo.');
    } finally {
      setConfirmando(false);
    }
  };

  const verCameras = () => {
    alertaAtivoStore.dispensar();
    if (navigationRef.isReady()) {
      navigationRef.navigate('MainTabs', { screen: 'Cameras' });
    }
  };

  return (
    <Animated.View
      entering={FadeIn.duration(180)}
      style={[styles.fundo, { paddingTop: insets.top + spacing.lg, paddingBottom: insets.bottom + spacing.lg }]}
      accessibilityViewIsModal
    >
      <ScrollView contentContainerStyle={styles.conteudo}>
        <View style={styles.topo}>
          <Animated.View style={[styles.icone, estiloPulso]}>
            <Siren size={22} color={colors.danger} />
          </Animated.View>
          <Pressable onPress={alertaAtivoStore.dispensar} style={styles.fechar} accessibilityLabel="Fechar sem confirmar" hitSlop={8}>
            <X size={18} color={colors.sub} />
          </Pressable>
        </View>

        <Text style={styles.titulo} accessibilityRole="header">
          {alerta.titulo.toUpperCase()}
        </Text>
        <Text style={styles.local}>
          {nomePropriedade ? `${nomePropriedade} · ` : ''}às {hora}
        </Text>

        <View style={styles.cartao}>
          <Text style={styles.descricao}>{alerta.descricao}</Text>
          <Text style={styles.situacao}>{situacaoEscalonamento(alerta)}</Text>
        </View>

        <PrimaryButton label="Estou ciente" icone={CheckCircle2} onPress={confirmar} loading={confirmando} />
        {erro ? <Text style={styles.erro}>{erro}</Text> : null}

        <View style={styles.acoes}>
          <PrimaryButton label="Ver câmeras" icone={Camera} variant="secondary" onPress={verCameras} />
          {contatos.map((c) => (
            <PrimaryButton
              key={c.id}
              label={`Ligar para ${c.nome} · ${formatarTelefone(c.telefone)}`}
              icone={Phone}
              variant="secondary"
              onPress={() => Linking.openURL(`tel:${c.telefone}`)}
            />
          ))}
          <PrimaryButton label="Ligar 190 (Polícia)" icone={Phone} variant="danger" onPress={() => Linking.openURL('tel:190')} />
        </View>
      </ScrollView>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  fundo: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: colors.bg,
    zIndex: 100,
    elevation: 100,
  },
  conteudo: { paddingHorizontal: spacing.xl, gap: spacing.md, maxWidth: 520, width: '100%', alignSelf: 'center' },
  topo: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  icone: {
    width: 48,
    height: 48,
    borderRadius: radius.lg,
    backgroundColor: colors.dangerDim,
    borderWidth: 1,
    borderColor: colors.dangerLine,
    alignItems: 'center',
    justifyContent: 'center',
  },
  fechar: {
    width: 44,
    height: 44,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.danger, fontSize: fontSize.title, fontWeight: fontWeight.black, letterSpacing: 0.5, marginTop: spacing.sm },
  local: { color: colors.sub, fontSize: fontSize.secondary },
  cartao: {
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.dangerLine,
    backgroundColor: colors.surface,
    padding: spacing.lg,
    gap: spacing.sm,
    marginVertical: spacing.sm,
  },
  descricao: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.bold },
  situacao: { color: colors.sub, fontSize: fontSize.secondary },
  erro: { color: colors.danger, fontSize: fontSize.secondary, textAlign: 'center' },
  acoes: { gap: spacing.sm, marginTop: spacing.sm },
});
