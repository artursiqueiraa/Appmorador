import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, Lock, ShieldAlert, ShieldCheck, Unlock, Wifi, WifiOff } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type {
  CentralIntelbrasResponse,
  ResultadoComandoIntelbras,
  ResultadoTesteConexaoIntelbras,
  StatusCentralIntelbrasInfo,
} from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Skeleton } from '../../components/Skeleton';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type DetalhesCentralIntelbrasRouteProp = RouteProp<RootStackParamList, 'DetalhesCentralIntelbras'>;

/**
 * Sprint 15 — Integração Intelbras: Prova Definitiva da Arquitetura (ADR 0018).
 * Cada ação chama um comando real na central via `IIntelbrasComandoServico`
 * (backend, API HTTP local) — mesma forma de tela de DetalhesCentralJflScreen, sem
 * PGM/inibição de zona (não implementados nesta Sprint, ver dívida técnica).
 */
export function DetalhesCentralIntelbrasScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<DetalhesCentralIntelbrasRouteProp>();
  const { equipamentoId } = params;

  const [central, setCentral] = useState<CentralIntelbrasResponse | null>(null);
  const [status, setStatus] = useState<StatusCentralIntelbrasInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [mensagem, setMensagem] = useState<string | null>(null);
  const [processando, setProcessando] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const dados = await api.get<CentralIntelbrasResponse>(`/api/equipamentos/${equipamentoId}/intelbras`);
      setCentral(dados);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar a central.');
    } finally {
      setLoading(false);
    }
  }, [equipamentoId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const executar = async (
    acao: string,
    tarefa: () => Promise<ResultadoComandoIntelbras | ResultadoTesteConexaoIntelbras>,
    sucessoMsg: string,
  ) => {
    setProcessando(acao);
    setError(null);
    setMensagem(null);
    try {
      const resultado = await tarefa();
      if (resultado.sucesso) {
        setMensagem(sucessoMsg);
        if ('statusResultante' in resultado && resultado.statusResultante) {
          setStatus(resultado.statusResultante);
        }
      } else {
        setError(resultado.mensagemErro ?? 'Não foi possível concluir a ação.');
      }
      await carregar();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível concluir a ação.');
    } finally {
      setProcessando(null);
    }
  };

  const testarConexao = () =>
    executar(
      'testar-conexao',
      () => api.post<ResultadoTesteConexaoIntelbras>(`/api/equipamentos/${equipamentoId}/intelbras/testar-conexao`),
      'Conexão bem-sucedida.',
    );

  const consultarStatus = () =>
    executar('status', () => api.get<ResultadoComandoIntelbras>(`/api/equipamentos/${equipamentoId}/intelbras/status`), 'Status atualizado.');

  const armarParticao = (numero: number) =>
    executar(
      `armar-${numero}`,
      () => api.post<ResultadoComandoIntelbras>(`/api/equipamentos/${equipamentoId}/intelbras/armar`, { particao: numero }),
      `Partição ${numero} armada.`,
    );

  const desarmarParticao = (numero: number) =>
    executar(
      `desarmar-${numero}`,
      () => api.post<ResultadoComandoIntelbras>(`/api/equipamentos/${equipamentoId}/intelbras/desarmar`, { particao: numero }),
      `Partição ${numero} desarmada.`,
    );

  const importarEventos = () =>
    executar(
      'eventos-importar',
      () => api.post<ResultadoTesteConexaoIntelbras>(`/api/equipamentos/${equipamentoId}/intelbras/eventos/importar`),
      'Eventos importados — veja na Central de Eventos.',
    );

  if (loading || !central) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
            <ChevronLeft size={20} color={colors.text} />
          </Pressable>
          <Text style={styles.title}>Detalhes da central</Text>
        </View>
        <Skeleton height={200} radius={radius.lg} />
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Detalhes da central</Text>
          <Text style={styles.subtitle}>{central.nome}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}
      {mensagem ? <Text style={styles.mensagem}>{mensagem}</Text> : null}

      <View style={styles.card}>
        <View style={styles.cardTopRow}>
          <View style={styles.cardIcon}>
            <ShieldCheck size={20} color={colors.safe} />
          </View>
          <View style={styles.cardTextWrap}>
            <Text style={styles.cardTitle}>{central.modelo ?? 'Central Intelbras'}</Text>
            <Text style={styles.cardSubtitle}>
              {status?.temProblemaAtivo ? 'Problema ativo detectado' : 'Nenhum problema ativo'}
            </Text>
          </View>
          {central.status === 'Online' ? (
            <View style={styles.statusRow}>
              <Wifi size={16} color={colors.safe} />
              <Text style={[styles.statusLabel, { color: colors.safe }]}>Online</Text>
            </View>
          ) : (
            <View style={styles.statusRow}>
              <WifiOff size={16} color={colors.mute} />
              <Text style={[styles.statusLabel, { color: colors.mute }]}>{central.status === 'Offline' ? 'Offline' : 'Desconhecido'}</Text>
            </View>
          )}
        </View>

        {central.ultimaSincronizacaoUtc ? (
          <Text style={styles.campo}>Última comunicação: {formatRelativeTime(central.ultimaSincronizacaoUtc)}</Text>
        ) : null}

        {status?.temProblemaAtivo ? (
          <View style={styles.problemaBadge}>
            <ShieldAlert size={12} color={colors.warn} />
            <Text style={styles.problemaTexto}>Problema ativo</Text>
          </View>
        ) : null}
      </View>

      <View style={styles.acoesTopo}>
        <PrimaryButton label="Testar conexão" onPress={testarConexao} loading={processando === 'testar-conexao'} />
        <PrimaryButton label="Consultar status" variant="secondary" onPress={consultarStatus} loading={processando === 'status'} />
        <PrimaryButton label="Importar eventos" variant="secondary" onPress={importarEventos} loading={processando === 'eventos-importar'} />
      </View>

      {status ? (
        <>
          <Text style={styles.secaoTitulo}>Partições</Text>
          <View style={styles.listaCartoes}>
            {status.particoes.map((particao) => (
              <View key={particao.numero} style={styles.itemLinha}>
                <View style={styles.itemTextoWrap}>
                  <Text style={styles.itemTitulo}>Partição {particao.numero}</Text>
                  <Text style={styles.itemSubtitulo}>{particao.armada ? 'Armada' : 'Desarmada'}</Text>
                </View>
                <View style={styles.itemAcoes}>
                  <Pressable
                    onPress={() => armarParticao(particao.numero)}
                    style={styles.itemBotao}
                    accessibilityLabel={`Armar partição ${particao.numero}`}
                  >
                    <Lock size={16} color={colors.safe} />
                  </Pressable>
                  <Pressable
                    onPress={() => desarmarParticao(particao.numero)}
                    style={styles.itemBotao}
                    accessibilityLabel={`Desarmar partição ${particao.numero}`}
                  >
                    <Unlock size={16} color={colors.warn} />
                  </Pressable>
                </View>
              </View>
            ))}
          </View>
        </>
      ) : null}
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
  statusRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs },
  statusLabel: { fontSize: fontSize.label, fontWeight: fontWeight.medium },
  campo: { color: colors.sub, fontSize: fontSize.secondary },
  problemaBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: 4,
    paddingHorizontal: spacing.xs + 2,
    paddingVertical: 4,
    borderRadius: radius.pill,
    backgroundColor: colors.warnDim,
    borderWidth: 1,
    borderColor: colors.warnLine,
  },
  problemaTexto: { color: colors.warn, fontSize: fontSize.tiny },
  acoesTopo: { gap: spacing.sm, marginBottom: spacing.lg },
  secaoTitulo: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, marginBottom: spacing.sm, marginTop: spacing.sm },
  listaCartoes: { gap: spacing.xs, marginBottom: spacing.md },
  itemLinha: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  itemTextoWrap: { flex: 1 },
  itemTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  itemSubtitulo: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  itemAcoes: { flexDirection: 'row', gap: spacing.sm },
  itemBotao: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
  mensagem: { color: colors.safe, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
