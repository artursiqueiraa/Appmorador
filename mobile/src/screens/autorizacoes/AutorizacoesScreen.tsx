import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { CalendarClock, ChevronLeft, Trash2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { AutorizacaoResponse, MoradorResponse, StatusAutorizacao, TipoVisita, UnidadeResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { TipoVisitaSelector, rotuloTipoVisita } from '../../components/TipoVisitaSelector';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type AutorizacoesRouteProp = RouteProp<RootStackParamList, 'Autorizacoes'>;

const DATA_REGEX = /^\d{4}-\d{2}-\d{2}$/;
const HORARIO_REGEX = /^([01]?\d|2[0-3]):([0-5]\d)$/;

const STATUS_COR: Record<StatusAutorizacao, string> = {
  Pendente: colors.accent,
  Ativa: colors.safe,
  Expirada: colors.mute,
  Cancelada: colors.danger,
  Utilizada: colors.mute,
};

function formatarData(dataIso: string): string {
  return dataIso.slice(0, 10);
}

function formatarHorario(horario?: string | null): string {
  return horario ? horario.slice(0, 5) : '';
}

/**
 * Sprint 8 — Visitantes e Autorizações. Unidade/Morador responsável são escolhidos só
 * na criação (imutáveis depois, mesmo espírito de Credencial.Tipo — ADR 0010/0011).
 * Status Pendente/Ativa/Expirada é computado pelo backend a partir das datas — nunca
 * enviado pelo app; só Cancelada/Utilizada são ações explícitas do usuário.
 */
export function AutorizacoesScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<AutorizacoesRouteProp>();
  const { visitanteId, nomeVisitante, propriedadeId } = params;

  const [autorizacoes, setAutorizacoes] = useState<AutorizacaoResponse[]>([]);
  const [unidades, setUnidades] = useState<UnidadeResponse[]>([]);
  const [moradoresDaUnidade, setMoradoresDaUnidade] = useState<MoradorResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<AutorizacaoResponse | null>(null);
  const [unidadeId, setUnidadeId] = useState<string | null>(null);
  const [moradorResponsavelId, setMoradorResponsavelId] = useState<string | null>(null);
  const [tipo, setTipo] = useState<TipoVisita | null>(null);
  const [dataInicial, setDataInicial] = useState('');
  const [dataFinal, setDataFinal] = useState('');
  const [horarioInicial, setHorarioInicial] = useState('');
  const [horarioFinal, setHorarioFinal] = useState('');
  const [erroValidacao, setErroValidacao] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [listaAutorizacoes, listaUnidades] = await Promise.all([
        api.get<AutorizacaoResponse[]>(`/api/visitantes/${visitanteId}/autorizacoes`),
        api.get<UnidadeResponse[]>(`/api/properties/${propriedadeId}/unidades`),
      ]);
      setAutorizacoes(listaAutorizacoes);
      setUnidades(listaUnidades);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar as autorizações.');
    } finally {
      setLoading(false);
    }
  }, [visitanteId, propriedadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const carregarMoradoresDaUnidade = useCallback(async (idUnidade: string) => {
    try {
      const lista = await api.get<MoradorResponse[]>(`/api/unidades/${idUnidade}/moradores`);
      setMoradoresDaUnidade(lista);
    } catch {
      setMoradoresDaUnidade([]);
    }
  }, []);

  const selecionarUnidade = (idUnidade: string) => {
    setUnidadeId(idUnidade);
    setMoradorResponsavelId(null);
    carregarMoradoresDaUnidade(idUnidade);
  };

  const abrirNovo = () => {
    setEditando(null);
    setUnidadeId(null);
    setMoradorResponsavelId(null);
    setMoradoresDaUnidade([]);
    setTipo(null);
    setDataInicial('');
    setDataFinal('');
    setHorarioInicial('');
    setHorarioFinal('');
    setErroValidacao(null);
    setShowForm(true);
  };

  const abrirEdicao = (autorizacao: AutorizacaoResponse) => {
    setEditando(autorizacao);
    setTipo(autorizacao.tipo);
    setDataInicial(formatarData(autorizacao.dataInicial));
    setDataFinal(formatarData(autorizacao.dataFinal));
    setHorarioInicial(formatarHorario(autorizacao.horarioInicial));
    setHorarioFinal(formatarHorario(autorizacao.horarioFinal));
    setErroValidacao(null);
    setShowForm(true);
  };

  const validarDatasEHorarios = (): boolean => {
    if (!DATA_REGEX.test(dataInicial) || !DATA_REGEX.test(dataFinal)) {
      setErroValidacao('Use o formato AAAA-MM-DD para as datas.');
      return false;
    }
    if (horarioInicial && !HORARIO_REGEX.test(horarioInicial)) {
      setErroValidacao('Use o formato HH:MM para o horário inicial, ex.: 08:00.');
      return false;
    }
    if (horarioFinal && !HORARIO_REGEX.test(horarioFinal)) {
      setErroValidacao('Use o formato HH:MM para o horário final, ex.: 18:00.');
      return false;
    }
    setErroValidacao(null);
    return true;
  };

  const salvar = async () => {
    if (!editando && (!unidadeId || !moradorResponsavelId)) {
      return;
    }
    if (!tipo || !validarDatasEHorarios()) {
      return;
    }

    const payload = {
      tipo,
      dataInicial: `${dataInicial}T00:00:00Z`,
      dataFinal: `${dataFinal}T23:59:59Z`,
      horarioInicial: horarioInicial ? `${horarioInicial}:00` : undefined,
      horarioFinal: horarioFinal ? `${horarioFinal}:00` : undefined,
    };

    setSalvando(true);
    setError(null);
    try {
      if (editando) {
        const atualizada = await api.put<AutorizacaoResponse>(`/api/autorizacoes/${editando.id}`, payload);
        setAutorizacoes((prev) => prev.map((a) => (a.id === atualizada.id ? atualizada : a)));
      } else {
        const criada = await api.post<AutorizacaoResponse>(`/api/visitantes/${visitanteId}/autorizacoes`, {
          unidadeId,
          moradorResponsavelId,
          ...payload,
        });
        setAutorizacoes((prev) => [...prev, criada]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar a autorização.');
    } finally {
      setSalvando(false);
    }
  };

  const marcarUtilizada = async (autorizacao: AutorizacaoResponse) => {
    try {
      const atualizada = await api.put<AutorizacaoResponse>(`/api/autorizacoes/${autorizacao.id}/status`, { status: 'Utilizada' });
      setAutorizacoes((prev) => prev.map((a) => (a.id === atualizada.id ? atualizada : a)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível atualizar a autorização.');
    }
  };

  const confirmarCancelamento = (autorizacao: AutorizacaoResponse) => {
    Alert.alert(
      'Cancelar autorização?',
      `A autorização de ${rotuloTipoVisita(autorizacao.tipo)} para "${autorizacao.unidadeIdentificacao}" será cancelada.`,
      [
        { text: 'Voltar', style: 'cancel' },
        {
          text: 'Cancelar autorização',
          style: 'destructive',
          onPress: async () => {
            try {
              const atualizada = await api.put<AutorizacaoResponse>(`/api/autorizacoes/${autorizacao.id}/status`, { status: 'Cancelada' });
              setAutorizacoes((prev) => prev.map((a) => (a.id === atualizada.id ? atualizada : a)));
            } catch (err) {
              setError(err instanceof ApiError ? err.message : 'Não foi possível cancelar a autorização.');
            }
          },
        },
      ],
    );
  };

  const confirmarExclusao = (autorizacao: AutorizacaoResponse) => {
    Alert.alert('Excluir autorização?', 'Esta autorização deixará de aparecer no app.', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Excluir',
        style: 'destructive',
        onPress: async () => {
          try {
            await api.delete(`/api/autorizacoes/${autorizacao.id}`);
            setAutorizacoes((prev) => prev.filter((a) => a.id !== autorizacao.id));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível excluir a autorização.');
          }
        },
      },
    ]);
  };

  const semUnidadesCadastradas = !loading && unidades.length === 0;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Autorizações</Text>
          <Text style={styles.subtitle}>{nomeVisitante}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {loading ? (
        <View style={{ gap: spacing.sm }}>
          <Skeleton height={80} radius={radius.lg} />
          <Skeleton height={80} radius={radius.lg} />
        </View>
      ) : (
        <FlatList
          data={autorizacoes}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => {
            const editavel = item.status !== 'Cancelada' && item.status !== 'Utilizada';
            return (
              <View style={styles.card}>
                <View style={styles.cardHeader}>
                  <View style={styles.cardIcon}>
                    <CalendarClock size={18} color={STATUS_COR[item.status]} />
                  </View>
                  <View style={styles.cardTextWrap}>
                    <Text style={styles.cardTitle}>{rotuloTipoVisita(item.tipo)}</Text>
                    <Text style={styles.cardSubtitle}>
                      {item.unidadeIdentificacao} • Responsável: {item.moradorResponsavelNome}
                    </Text>
                    <Text style={styles.cardSubtitle}>
                      {formatarData(item.dataInicial)} até {formatarData(item.dataFinal)}
                      {item.horarioInicial ? ` • ${formatarHorario(item.horarioInicial)}–${formatarHorario(item.horarioFinal)}` : ''}
                    </Text>
                  </View>
                  <Text style={[styles.statusLabel, { color: STATUS_COR[item.status] }]}>{item.status}</Text>
                </View>
                {editavel ? (
                  <View style={styles.cardActions}>
                    <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn}>
                      <Text style={styles.actionLabel}>Editar</Text>
                    </Pressable>
                    <Pressable onPress={() => marcarUtilizada(item)} style={styles.actionBtn}>
                      <Text style={styles.actionLabel}>Marcar utilizada</Text>
                    </Pressable>
                    <Pressable onPress={() => confirmarCancelamento(item)} style={styles.actionBtn}>
                      <Text style={[styles.actionLabel, { color: colors.danger }]}>Cancelar</Text>
                    </Pressable>
                  </View>
                ) : null}
                <View style={styles.cardActions}>
                  <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir autorização">
                    <Trash2 size={16} color={colors.danger} />
                  </Pressable>
                </View>
              </View>
            );
          }}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={CalendarClock}
                titulo="Nenhuma autorização ainda"
                descricao="Crie uma autorização definindo a unidade, o morador responsável e o período de validade."
                cta={{ label: 'Adicionar autorização', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          {editando ? (
            <Text style={styles.fixo}>
              {editando.unidadeIdentificacao} • Responsável: {editando.moradorResponsavelNome}
            </Text>
          ) : (
            <>
              <View style={styles.selectorBlock}>
                <Text style={styles.label}>Unidade</Text>
                <View style={styles.chipsRow}>
                  {unidades.map((u) => {
                    const ativo = u.id === unidadeId;
                    return (
                      <Pressable key={u.id} onPress={() => selecionarUnidade(u.id)} style={[styles.chip, ativo && styles.chipAtivo]}>
                        <Text style={[styles.chipLabel, ativo && styles.chipLabelAtivo]}>{u.identificacao}</Text>
                      </Pressable>
                    );
                  })}
                </View>
              </View>

              {unidadeId ? (
                <View style={styles.selectorBlock}>
                  <Text style={styles.label}>Morador responsável</Text>
                  <View style={styles.chipsRow}>
                    {moradoresDaUnidade.map((m) => {
                      const ativo = m.id === moradorResponsavelId;
                      return (
                        <Pressable
                          key={m.id}
                          onPress={() => setMoradorResponsavelId(m.id)}
                          style={[styles.chip, ativo && styles.chipAtivo]}
                        >
                          <Text style={[styles.chipLabel, ativo && styles.chipLabelAtivo]}>{m.nome}</Text>
                        </Pressable>
                      );
                    })}
                  </View>
                </View>
              ) : null}
            </>
          )}

          <TipoVisitaSelector label="Tipo de visita" value={tipo} onChange={setTipo} />

          <TextField label="Data inicial" value={dataInicial} onChangeText={setDataInicial} placeholder="2026-07-21" />
          <TextField label="Data final" value={dataFinal} onChangeText={setDataFinal} placeholder="2026-07-22" error={erroValidacao ?? undefined} />
          <TextField label="Horário inicial (opcional)" value={horarioInicial} onChangeText={setHorarioInicial} placeholder="08:00" />
          <TextField label="Horário final (opcional)" value={horarioFinal} onChangeText={setHorarioFinal} placeholder="18:00" />

          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Criar autorização'}
            onPress={salvar}
            loading={salvando}
            disabled={(!editando && (!unidadeId || !moradorResponsavelId)) || !tipo || !dataInicial || !dataFinal}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : semUnidadesCadastradas ? (
        <Text style={styles.avisoVazio}>Cadastre uma unidade na propriedade antes de criar uma autorização.</Text>
      ) : (
        <PrimaryButton label="Criar autorização" variant="secondary" onPress={abrirNovo} />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: spacing.xl },
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
  list: { paddingBottom: spacing.lg, gap: spacing.sm },
  card: {
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
    gap: spacing.xs,
  },
  cardHeader: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.md },
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
  cardActions: { flexDirection: 'row', gap: spacing.md, flexWrap: 'wrap' },
  actionBtn: { minHeight: 28, alignItems: 'center', justifyContent: 'center' },
  actionLabel: { color: colors.sub, fontSize: fontSize.label, fontWeight: fontWeight.medium },
  form: { gap: spacing.sm },
  selectorBlock: { marginBottom: spacing.lg },
  label: {
    color: colors.sub,
    fontSize: fontSize.meta,
    fontWeight: fontWeight.medium,
    marginBottom: spacing.xs + 2,
  },
  fixo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium, marginBottom: spacing.md },
  chipsRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs },
  chip: {
    paddingVertical: spacing.xs + 2,
    paddingHorizontal: spacing.md,
    borderRadius: radius.pill,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  chipAtivo: { backgroundColor: colors.safeDim, borderColor: colors.safeLine },
  chipLabel: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  chipLabelAtivo: { color: colors.safe },
  avisoVazio: { color: colors.mute, fontSize: fontSize.secondary, textAlign: 'center', marginTop: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
