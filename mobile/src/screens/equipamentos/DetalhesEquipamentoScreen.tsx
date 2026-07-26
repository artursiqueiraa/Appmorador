import React, { useCallback, useEffect, useState } from 'react';
import { ScrollView, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, Router, Wifi, WifiOff } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type {
  EquipamentoResponse,
  ImportacaoEventosResponse,
  InformacoesEquipamentoResponse,
  SincronizacaoResponse,
  TesteConexaoResponse,
} from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { rotuloFabricanteEquipamento } from '../../components/FabricanteEquipamentoSelector';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Skeleton } from '../../components/Skeleton';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type DetalhesEquipamentoRouteProp = RouteProp<RootStackParamList, 'DetalhesEquipamento'>;

/**
 * Sprint 11 — Migração da Integração Control iD (ADR 0014). Cada botão chama uma ação
 * de integração real (nunca simulada no mobile) via `IEquipamentoIntegracaoServico` no
 * backend. Resultado de cada ação some mostrado inline — sem navegação extra.
 *
 * Sprint 17 (ADR 0020) — RBAC de UI: um morador não precisa (e não deveria) ver
 * IP/usuário/identificador nem botões de sincronização técnica (achado #5 da
 * auditoria). Como o domínio ainda não tem RBAC real (`perfil` é só uma preferência
 * local, ver `auth/profilePreference.ts`), a tela inteira nunca deixa de existir —
 * só o técnico vê a versão completa; o morador vê ícone + nome + estado + "Ver
 * histórico".
 */
export function DetalhesEquipamentoScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<DetalhesEquipamentoRouteProp>();
  const { equipamentoId } = params;
  const { perfil } = useAuth();

  const [equipamento, setEquipamento] = useState<EquipamentoResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [processando, setProcessando] = useState<string | null>(null);
  const [mensagem, setMensagem] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const dados = await api.get<EquipamentoResponse>(`/api/equipamentos/${equipamentoId}`);
      setEquipamento(dados);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar o equipamento.');
    } finally {
      setLoading(false);
    }
  }, [equipamentoId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const executar = async (acao: string, tarefa: () => Promise<void>) => {
    setProcessando(acao);
    setError(null);
    setMensagem(null);
    try {
      await tarefa();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível concluir a ação.');
    } finally {
      setProcessando(null);
    }
  };

  const testarConexao = () =>
    executar('testar-conexao', async () => {
      const resultado = await api.post<TesteConexaoResponse>(`/api/equipamentos/${equipamentoId}/testar-conexao`);
      setMensagem(resultado.sucesso ? 'Conexão bem-sucedida.' : resultado.mensagemErro ?? 'Não foi possível conectar.');
      await carregar();
    });

  const consultarInformacoes = () =>
    executar('informacoes', async () => {
      const resultado = await api.get<InformacoesEquipamentoResponse>(`/api/equipamentos/${equipamentoId}/informacoes`);
      setMensagem(
        `Versão ${resultado.versao}${resultado.nomeDispositivo ? ` · ${resultado.nomeDispositivo}` : ''}${
          resultado.numeroSerie ? ` · Nº ${resultado.numeroSerie}` : ''
        }`,
      );
      await carregar();
    });

  const sincronizar = (acao: string, path: string, rotulo: string) =>
    executar(acao, async () => {
      const resultado = await api.post<SincronizacaoResponse>(`/api/equipamentos/${equipamentoId}/${path}`);
      setMensagem(`${rotulo}: ${resultado.quantidadeProcessada} sincronizado(s).`);
      await carregar();
    });

  const importarEventos = () =>
    executar('importar-eventos', async () => {
      const resultado = await api.post<ImportacaoEventosResponse>(`/api/equipamentos/${equipamentoId}/importar-eventos`);
      setMensagem(`${resultado.quantidadeImportada} evento(s) importado(s).`);
      await carregar();
    });

  if (loading || !equipamento) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
            <ChevronLeft size={20} color={colors.text} />
          </Pressable>
          <Text style={styles.title}>Detalhes do equipamento</Text>
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
          <Text style={styles.title}>Detalhes do equipamento</Text>
          <Text style={styles.subtitle}>{equipamento.nome}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}
      {mensagem ? <Text style={styles.mensagem}>{mensagem}</Text> : null}

      <View style={styles.card}>
        <View style={styles.cardTopRow}>
          <View style={styles.cardIcon}>
            <Router size={20} color={colors.safe} />
          </View>
          <View style={styles.cardTextWrap}>
            <Text style={styles.cardTitle}>{perfil === 'tecnico' ? rotuloFabricanteEquipamento(equipamento.fabricante) : equipamento.nome}</Text>
            {perfil === 'tecnico' ? (
              <Text style={styles.cardSubtitle}>
                {equipamento.ip}:{equipamento.porta}
              </Text>
            ) : null}
          </View>
          {equipamento.status === 'Online' ? (
            <View style={styles.statusRow}>
              <Wifi size={16} color={colors.safe} />
              <Text style={[styles.statusLabel, { color: colors.safe }]}>Online</Text>
            </View>
          ) : (
            <View style={styles.statusRow}>
              <WifiOff size={16} color={colors.mute} />
              <Text style={[styles.statusLabel, { color: colors.mute }]}>{equipamento.status === 'Offline' ? 'Offline' : 'Desconhecido'}</Text>
            </View>
          )}
        </View>

        {perfil === 'tecnico' ? (
          <>
            {equipamento.modelo ? <Text style={styles.campo}>Modelo: {equipamento.modelo}</Text> : null}
            {equipamento.identificador ? <Text style={styles.campo}>Identificador: {equipamento.identificador}</Text> : null}
            <Text style={styles.campo}>Usuário: {equipamento.usuario}</Text>
          </>
        ) : null}
        <Text style={styles.campo}>
          Última sincronização: {equipamento.ultimaSincronizacaoUtc ? formatRelativeTime(equipamento.ultimaSincronizacaoUtc) : 'Nunca'}
        </Text>
      </View>

      {perfil === 'tecnico' ? (
        <View style={styles.acoes}>
          <PrimaryButton label="Testar conexão" onPress={testarConexao} loading={processando === 'testar-conexao'} />
          <PrimaryButton
            label="Consultar informações"
            variant="secondary"
            onPress={consultarInformacoes}
            loading={processando === 'informacoes'}
          />
          <PrimaryButton
            label="Sincronizar moradores"
            variant="secondary"
            onPress={() => sincronizar('sincronizar-moradores', 'sincronizar-moradores', 'Moradores')}
            loading={processando === 'sincronizar-moradores'}
          />
          <PrimaryButton
            label="Sincronizar credenciais"
            variant="secondary"
            onPress={() => sincronizar('sincronizar-credenciais', 'sincronizar-credenciais', 'Credenciais')}
            loading={processando === 'sincronizar-credenciais'}
          />
          <PrimaryButton
            label="Sincronizar permissões"
            variant="secondary"
            onPress={() => sincronizar('sincronizar-permissoes', 'sincronizar-permissoes', 'Permissões')}
            loading={processando === 'sincronizar-permissoes'}
          />
          <PrimaryButton label="Importar eventos" variant="secondary" onPress={importarEventos} loading={processando === 'importar-eventos'} />
        </View>
      ) : (
        <View style={styles.acoes}>
          <PrimaryButton label="Ver histórico" variant="secondary" onPress={() => navigation.navigate('Eventos')} />
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
  statusRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs },
  statusLabel: { fontSize: fontSize.label, fontWeight: fontWeight.medium },
  campo: { color: colors.sub, fontSize: fontSize.secondary },
  acoes: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
  mensagem: { color: colors.safe, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
