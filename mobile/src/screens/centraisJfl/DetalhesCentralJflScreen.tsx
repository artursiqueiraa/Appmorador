import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, Lock, ShieldAlert, ShieldCheck, Unlock, Wifi, WifiOff, Zap, ZapOff } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { CentralJflResponse, ResultadoComandoJfl, ResultadoTesteConexaoJfl, StatusCentralJflInfo } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { Skeleton } from '../../components/Skeleton';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type DetalhesCentralJflRouteProp = RouteProp<RootStackParamList, 'DetalhesCentralJfl'>;

/**
 * Sprint 12 — Migração JFL Active 100 Bus (ADR 0015). Cada ação chama um comando
 * real na central via `IJflComandoServico` (backend) — a central precisa estar
 * conectada (sessão TCP já aberta) para qualquer ação funcionar, exceto o cadastro.
 */
export function DetalhesCentralJflScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<DetalhesCentralJflRouteProp>();
  const { equipamentoId } = params;

  const [central, setCentral] = useState<CentralJflResponse | null>(null);
  const [status, setStatus] = useState<StatusCentralJflInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [mensagem, setMensagem] = useState<string | null>(null);
  const [processando, setProcessando] = useState<string | null>(null);
  const [zonaInput, setZonaInput] = useState('');

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const dados = await api.get<CentralJflResponse>(`/api/equipamentos/${equipamentoId}/jfl`);
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

  const executar = async (acao: string, tarefa: () => Promise<ResultadoComandoJfl | ResultadoTesteConexaoJfl>, sucessoMsg: string) => {
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
      () => api.post<ResultadoTesteConexaoJfl>(`/api/equipamentos/${equipamentoId}/jfl/testar-conexao`),
      'Conexão bem-sucedida.',
    );

  const consultarStatus = () =>
    executar('status', () => api.get<ResultadoComandoJfl>(`/api/equipamentos/${equipamentoId}/jfl/status`), 'Status atualizado.');

  const armarParticao = (numero: number) =>
    executar(
      `armar-${numero}`,
      () => api.post<ResultadoComandoJfl>(`/api/equipamentos/${equipamentoId}/jfl/armar`, { particao: numero }),
      `Partição ${numero} armada.`,
    );

  const desarmarParticao = (numero: number) =>
    executar(
      `desarmar-${numero}`,
      () => api.post<ResultadoComandoJfl>(`/api/equipamentos/${equipamentoId}/jfl/desarmar`, { particao: numero }),
      `Partição ${numero} desarmada.`,
    );

  const acionarPgm = (numero: number) =>
    executar(
      `pgm-on-${numero}`,
      () => api.post<ResultadoComandoJfl>(`/api/equipamentos/${equipamentoId}/jfl/pgm/acionar`, { pgmNumero: numero }),
      `PGM ${numero} acionada.`,
    );

  const desligarPgm = (numero: number) =>
    executar(
      `pgm-off-${numero}`,
      () => api.post<ResultadoComandoJfl>(`/api/equipamentos/${equipamentoId}/jfl/pgm/desligar`, { pgmNumero: numero }),
      `PGM ${numero} desligada.`,
    );

  const inibirZona = () => {
    const numero = Number(zonaInput);
    if (!numero) {
      return;
    }
    executar(
      'zona-inibir',
      () => api.post<ResultadoComandoJfl>(`/api/equipamentos/${equipamentoId}/jfl/zonas/inibir`, { zonaNumero: numero }),
      `Zona ${numero} inibida.`,
    );
  };

  const desinibirZona = (numero: number) =>
    executar(
      `zona-desinibir-${numero}`,
      () => api.post<ResultadoComandoJfl>(`/api/equipamentos/${equipamentoId}/jfl/zonas/desinibir`, { zonaNumero: numero }),
      `Zona ${numero} desinibida.`,
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

  const particoesConfiguradas = status?.particoes.filter((p) => !p.desabilitada) ?? [];
  const pgmsPermitidas = status?.pgms.filter((p) => p.permitida) ?? [];
  const zonasInibidas = status?.zonas.filter((z) => z.estado === 'Inibida') ?? [];

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
            <Text style={styles.cardTitle}>Nº série {central.numeroSerie}</Text>
            {central.centralVinculadaNome ? (
              <Text style={styles.cardSubtitle}>Vinculada a &quot;{central.centralVinculadaNome}&quot; (eventos)</Text>
            ) : (
              <Text style={styles.cardSubtitle}>Sem central de eventos vinculada</Text>
            )}
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

        {status ? (
          <>
            <Text style={styles.campo}>
              Bateria: {status.bateriaTipo}
              {status.bateriaPercentual ? ` (${status.bateriaPercentual}%)` : ''}
              {status.bateriaTensaoAproximada ? ` (~${status.bateriaTensaoAproximada}V)` : ''}
            </Text>
            <Text style={styles.campo}>Eletrificador: {status.eletrificadorArmado ? 'Armado' : 'Desarmado'}</Text>
            {status.problemasAtivos.length > 0 ? (
              <View style={styles.problemasWrap}>
                {status.problemasAtivos.map((problema) => (
                  <View key={problema} style={styles.problemaBadge}>
                    <ShieldAlert size={12} color={colors.warn} />
                    <Text style={styles.problemaTexto}>{problema}</Text>
                  </View>
                ))}
              </View>
            ) : (
              <Text style={styles.campoOk}>Nenhum problema ativo</Text>
            )}
          </>
        ) : null}
      </View>

      <View style={styles.acoesTopo}>
        <PrimaryButton label="Testar conexão" onPress={testarConexao} loading={processando === 'testar-conexao'} />
        <PrimaryButton label="Consultar status" variant="secondary" onPress={consultarStatus} loading={processando === 'status'} />
      </View>

      {status ? (
        <>
          <Text style={styles.secaoTitulo}>Partições</Text>
          <View style={styles.listaCartoes}>
            {particoesConfiguradas.map((particao) => (
              <View key={particao.numero} style={styles.itemLinha}>
                <View style={styles.itemTextoWrap}>
                  <Text style={styles.itemTitulo}>Partição {particao.numero}</Text>
                  <Text style={styles.itemSubtitulo}>
                    {particao.armadaStay ? 'Armada (Stay)' : particao.armada ? 'Armada' : 'Desarmada'}
                    {particao.emDisparo ? ' · Em disparo' : ''}
                  </Text>
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

          {pgmsPermitidas.length > 0 ? (
            <>
              <Text style={styles.secaoTitulo}>PGMs</Text>
              <View style={styles.listaCartoes}>
                {pgmsPermitidas.map((pgm) => (
                  <View key={pgm.numero} style={styles.itemLinha}>
                    <View style={styles.itemTextoWrap}>
                      <Text style={styles.itemTitulo}>PGM {pgm.numero}</Text>
                      <Text style={styles.itemSubtitulo}>{pgm.acionada ? 'Acionada' : 'Desligada'}</Text>
                    </View>
                    <View style={styles.itemAcoes}>
                      <Pressable onPress={() => acionarPgm(pgm.numero)} style={styles.itemBotao} accessibilityLabel={`Acionar PGM ${pgm.numero}`}>
                        <Zap size={16} color={colors.safe} />
                      </Pressable>
                      <Pressable onPress={() => desligarPgm(pgm.numero)} style={styles.itemBotao} accessibilityLabel={`Desligar PGM ${pgm.numero}`}>
                        <ZapOff size={16} color={colors.warn} />
                      </Pressable>
                    </View>
                  </View>
                ))}
              </View>
            </>
          ) : null}

          <Text style={styles.secaoTitulo}>Zonas inibidas</Text>
          {zonasInibidas.length > 0 ? (
            <View style={styles.listaCartoes}>
              {zonasInibidas.map((zona) => (
                <View key={zona.numero} style={styles.itemLinha}>
                  <Text style={styles.itemTitulo}>Zona {zona.numero}</Text>
                  <Pressable
                    onPress={() => desinibirZona(zona.numero)}
                    style={styles.itemBotao}
                    accessibilityLabel={`Desinibir zona ${zona.numero}`}
                  >
                    <Text style={styles.desinibirTexto}>Desinibir</Text>
                  </Pressable>
                </View>
              ))}
            </View>
          ) : (
            <Text style={styles.campo}>Nenhuma zona inibida</Text>
          )}

          <View style={styles.formInibir}>
            <TextField
              label="Inibir zona (número)"
              value={zonaInput}
              onChangeText={setZonaInput}
              placeholder="Ex.: 5"
              keyboardType="number-pad"
            />
            <PrimaryButton label="Inibir zona" variant="secondary" onPress={inibirZona} loading={processando === 'zona-inibir'} disabled={!zonaInput} />
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
  campoOk: { color: colors.safe, fontSize: fontSize.secondary },
  problemasWrap: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs, marginTop: spacing.xs },
  problemaBadge: {
    flexDirection: 'row',
    alignItems: 'center',
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
  desinibirTexto: { color: colors.accent, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  formInibir: { gap: spacing.sm, marginTop: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
  mensagem: { color: colors.safe, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
