import React, { useCallback, useEffect, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, Package } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { EntregaResponse, StatusEntrega, TipoEntrega } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { TipoEntregaSelector, rotuloTipoEntrega } from '../../components/TipoEntregaSelector';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type DetalhesEntregaRouteProp = RouteProp<RootStackParamList, 'DetalhesEntrega'>;

const STATUS_COR: Record<StatusEntrega, string> = {
  AguardandoRecebimento: colors.accent,
  DisponivelParaRetirada: colors.safe,
  Retirada: colors.mute,
  Cancelada: colors.danger,
};

function formatarData(dataIso?: string | null): string | null {
  if (!dataIso) {
    return null;
  }
  const data = new Date(dataIso);
  return `${data.toLocaleDateString('pt-BR')} às ${data.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`;
}

/**
 * Sprint 10 — Entregas e Correspondências. Máquina de estados 100% manual (ver ADR
 * 0013): cada botão de ação corresponde a exatamente uma transição válida a partir do
 * status atual, sem cálculo/job por trás.
 */
export function DetalhesEntregaScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<DetalhesEntregaRouteProp>();
  const { entregaId } = params;

  const [entrega, setEntrega] = useState<EntregaResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [processando, setProcessando] = useState(false);

  const [editando, setEditando] = useState(false);
  const [tipo, setTipo] = useState<TipoEntrega | null>(null);
  const [descricao, setDescricao] = useState('');
  const [observacoes, setObservacoes] = useState('');

  const [mostrandoRecebimento, setMostrandoRecebimento] = useState(false);
  const [recebidoPor, setRecebidoPor] = useState('');

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const dados = await api.get<EntregaResponse>(`/api/entregas/${entregaId}`);
      setEntrega(dados);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar a entrega.');
    } finally {
      setLoading(false);
    }
  }, [entregaId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const abrirEdicao = () => {
    if (!entrega) {
      return;
    }
    setTipo(entrega.tipo);
    setDescricao(entrega.descricao ?? '');
    setObservacoes(entrega.observacoes ?? '');
    setEditando(true);
  };

  const salvarEdicao = async () => {
    if (!tipo) {
      return;
    }
    setProcessando(true);
    setError(null);
    try {
      const atualizada = await api.put<EntregaResponse>(`/api/entregas/${entregaId}`, {
        tipo,
        descricao: descricao.trim() || undefined,
        observacoes: observacoes.trim() || undefined,
      });
      setEntrega(atualizada);
      setEditando(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar a entrega.');
    } finally {
      setProcessando(false);
    }
  };

  const confirmarRecebimento = async () => {
    setProcessando(true);
    setError(null);
    try {
      const atualizada = await api.put<EntregaResponse>(`/api/entregas/${entregaId}/status`, {
        status: 'DisponivelParaRetirada',
        recebidoPor: recebidoPor.trim() || undefined,
      });
      setEntrega(atualizada);
      setMostrandoRecebimento(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível marcar a entrega como disponível.');
    } finally {
      setProcessando(false);
    }
  };

  const registrarRetirada = async () => {
    setProcessando(true);
    setError(null);
    try {
      const atualizada = await api.put<EntregaResponse>(`/api/entregas/${entregaId}/status`, { status: 'Retirada' });
      setEntrega(atualizada);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível registrar a retirada.');
    } finally {
      setProcessando(false);
    }
  };

  const confirmarCancelamento = () => {
    Alert.alert('Cancelar entrega?', 'Esta entrega será marcada como cancelada.', [
      { text: 'Voltar', style: 'cancel' },
      {
        text: 'Cancelar entrega',
        style: 'destructive',
        onPress: async () => {
          setProcessando(true);
          setError(null);
          try {
            const atualizada = await api.put<EntregaResponse>(`/api/entregas/${entregaId}/status`, { status: 'Cancelada' });
            setEntrega(atualizada);
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível cancelar a entrega.');
          } finally {
            setProcessando(false);
          }
        },
      },
    ]);
  };

  const confirmarExclusao = () => {
    Alert.alert('Excluir entrega?', 'Esta entrega deixará de aparecer no app.', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Excluir',
        style: 'destructive',
        onPress: async () => {
          try {
            await api.delete(`/api/entregas/${entregaId}`);
            navigation.goBack();
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível excluir a entrega.');
          }
        },
      },
    ]);
  };

  if (loading || !entrega) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
            <ChevronLeft size={20} color={colors.text} />
          </Pressable>
          <Text style={styles.title}>Detalhes da entrega</Text>
        </View>
        <Skeleton height={200} radius={radius.lg} />
      </View>
    );
  }

  const editavel = entrega.status !== 'Retirada' && entrega.status !== 'Cancelada';

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Detalhes da entrega</Text>
          <Text style={styles.subtitle}>{entrega.moradorDestinatarioNome}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <View style={styles.card}>
        <View style={styles.cardTopRow}>
          <View style={styles.cardIcon}>
            <Package size={20} color={STATUS_COR[entrega.status]} />
          </View>
          <View style={styles.cardTextWrap}>
            <Text style={styles.cardTitle}>{rotuloTipoEntrega(entrega.tipo)}</Text>
            <Text style={styles.cardSubtitle}>{entrega.unidadeIdentificacao}</Text>
          </View>
          <Text style={[styles.statusLabel, { color: STATUS_COR[entrega.status] }]}>{entrega.status}</Text>
        </View>

        {entrega.descricao ? <Text style={styles.campo}>Descrição: {entrega.descricao}</Text> : null}
        {entrega.recebidoPor ? <Text style={styles.campo}>Recebido por: {entrega.recebidoPor}</Text> : null}
        {formatarData(entrega.dataRecebimentoUtc) ? (
          <Text style={styles.campo}>Recebida em: {formatarData(entrega.dataRecebimentoUtc)}</Text>
        ) : null}
        {formatarData(entrega.dataRetiradaUtc) ? <Text style={styles.campo}>Retirada em: {formatarData(entrega.dataRetiradaUtc)}</Text> : null}
        {entrega.observacoes ? <Text style={styles.campo}>Observações: {entrega.observacoes}</Text> : null}
      </View>

      {editando ? (
        <View style={styles.form}>
          <TipoEntregaSelector label="Tipo" value={tipo} onChange={setTipo} />
          <TextField label="Descrição (opcional)" value={descricao} onChangeText={setDescricao} placeholder="Ex.: Caixa da Amazon" />
          <TextField label="Observações (opcional)" value={observacoes} onChangeText={setObservacoes} placeholder="Ex.: item frágil" />
          <PrimaryButton label="Salvar alterações" onPress={salvarEdicao} loading={processando} disabled={!tipo} />
          <PrimaryButton label="Cancelar edição" variant="secondary" onPress={() => setEditando(false)} />
        </View>
      ) : mostrandoRecebimento ? (
        <View style={styles.form}>
          <TextField label="Recebido por (opcional)" value={recebidoPor} onChangeText={setRecebidoPor} placeholder="Ex.: Portaria" />
          <PrimaryButton label="Confirmar recebimento" onPress={confirmarRecebimento} loading={processando} />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setMostrandoRecebimento(false)} />
        </View>
      ) : (
        <View style={styles.acoes}>
          {entrega.status === 'AguardandoRecebimento' ? (
            <PrimaryButton label="Marcar disponível para retirada" onPress={() => setMostrandoRecebimento(true)} />
          ) : null}
          {entrega.status === 'DisponivelParaRetirada' ? (
            <PrimaryButton label="Registrar retirada" onPress={registrarRetirada} loading={processando} />
          ) : null}
          {editavel ? <PrimaryButton label="Editar" variant="secondary" onPress={abrirEdicao} /> : null}
          {editavel ? <PrimaryButton label="Cancelar entrega" variant="secondary" onPress={confirmarCancelamento} /> : null}
          <PrimaryButton label="Excluir" variant="secondary" onPress={confirmarExclusao} />
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxl },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.lg },
  iconBtn: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerTextWrap: { flex: 1 },
  title: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold },
  subtitle: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  card: {
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.lg,
    gap: spacing.xs,
  },
  cardTopRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.xs },
  cardIcon: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cardTextWrap: { flex: 1 },
  cardTitle: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  cardSubtitle: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  statusLabel: { fontSize: fontSize.label, fontWeight: fontWeight.medium },
  campo: { color: colors.sub, fontSize: fontSize.secondary },
  form: { gap: spacing.sm },
  acoes: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
