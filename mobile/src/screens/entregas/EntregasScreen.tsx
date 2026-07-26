import React, { useCallback, useEffect, useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, ChevronRight, Package } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { EntregaResponse, MoradorResponse, StatusEntrega, TipoEntrega, UnidadeResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { TipoEntregaSelector, rotuloTipoEntrega } from '../../components/TipoEntregaSelector';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type EntregasRouteProp = RouteProp<RootStackParamList, 'Entregas'>;

const STATUS_COR: Record<StatusEntrega, string> = {
  AguardandoRecebimento: colors.accent,
  DisponivelParaRetirada: colors.safe,
  Retirada: colors.mute,
  Cancelada: colors.danger,
};

const STATUS_ROTULO: Record<StatusEntrega, string> = {
  AguardandoRecebimento: 'Aguardando',
  DisponivelParaRetirada: 'Disponível',
  Retirada: 'Retirada',
  Cancelada: 'Cancelada',
};

/**
 * Sprint 10 — Entregas e Correspondências. Visão unificada da propriedade (não por
 * morador individual — ver ADR 0013), com seleção em cascata Unidade→Morador
 * (mesmo padrão de `AutorizacoesScreen`, Sprint 8). Consulta detalhada e mudança de
 * status ficam em `DetalhesEntregaScreen`.
 */
export function EntregasScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<EntregasRouteProp>();
  const { propriedadeId, nomePropriedade } = params;

  const [entregas, setEntregas] = useState<EntregaResponse[]>([]);
  const [unidades, setUnidades] = useState<UnidadeResponse[]>([]);
  const [moradoresDaUnidade, setMoradoresDaUnidade] = useState<MoradorResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [unidadeId, setUnidadeId] = useState<string | null>(null);
  const [moradorDestinatarioId, setMoradorDestinatarioId] = useState<string | null>(null);
  const [tipo, setTipo] = useState<TipoEntrega | null>(null);
  const [descricao, setDescricao] = useState('');
  const [observacoes, setObservacoes] = useState('');
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [listaEntregas, listaUnidades] = await Promise.all([
        api.get<EntregaResponse[]>(`/api/properties/${propriedadeId}/entregas`),
        api.get<UnidadeResponse[]>(`/api/properties/${propriedadeId}/unidades`),
      ]);
      setEntregas(listaEntregas);
      setUnidades(listaUnidades);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar as entregas.');
    } finally {
      setLoading(false);
    }
  }, [propriedadeId]);

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
    setMoradorDestinatarioId(null);
    carregarMoradoresDaUnidade(idUnidade);
  };

  const abrirNovo = () => {
    setUnidadeId(null);
    setMoradorDestinatarioId(null);
    setMoradoresDaUnidade([]);
    setTipo(null);
    setDescricao('');
    setObservacoes('');
    setShowForm(true);
  };

  const salvar = async () => {
    if (!unidadeId || !moradorDestinatarioId || !tipo) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      const criada = await api.post<EntregaResponse>(`/api/properties/${propriedadeId}/entregas`, {
        unidadeId,
        moradorDestinatarioId,
        tipo,
        descricao: descricao.trim() || undefined,
        observacoes: observacoes.trim() || undefined,
      });
      setEntregas((prev) => [criada, ...prev]);
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível registrar a entrega.');
    } finally {
      setSalvando(false);
    }
  };

  const semUnidadesCadastradas = !loading && unidades.length === 0;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Entregas</Text>
          <Text style={styles.subtitle}>{nomePropriedade}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {loading ? (
        <View style={{ gap: spacing.sm }}>
          <Skeleton height={72} radius={radius.lg} />
          <Skeleton height={72} radius={radius.lg} />
        </View>
      ) : (
        <FlatList
          data={entregas}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => (
            <Pressable style={styles.card} onPress={() => navigation.navigate('DetalhesEntrega', { entregaId: item.id })}>
              <View style={styles.cardIcon}>
                <Package size={18} color={STATUS_COR[item.status]} />
              </View>
              <View style={styles.cardTextWrap}>
                <Text style={styles.cardTitle}>{rotuloTipoEntrega(item.tipo)}</Text>
                <Text style={styles.cardSubtitle}>
                  {item.moradorDestinatarioNome} • {item.unidadeIdentificacao}
                </Text>
              </View>
              <Text style={[styles.statusLabel, { color: STATUS_COR[item.status] }]}>{STATUS_ROTULO[item.status]}</Text>
              <ChevronRight size={18} color={colors.mute} />
            </Pressable>
          )}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={Package}
                titulo="Nenhuma entrega ainda"
                descricao="Registre uma entrega para acompanhar o recebimento e a retirada pelo morador."
                cta={{ label: 'Registrar entrega', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
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
              <Text style={styles.label}>Morador destinatário</Text>
              <View style={styles.chipsRow}>
                {moradoresDaUnidade.map((m) => {
                  const ativo = m.id === moradorDestinatarioId;
                  return (
                    <Pressable key={m.id} onPress={() => setMoradorDestinatarioId(m.id)} style={[styles.chip, ativo && styles.chipAtivo]}>
                      <Text style={[styles.chipLabel, ativo && styles.chipLabelAtivo]}>{m.nome}</Text>
                    </Pressable>
                  );
                })}
              </View>
            </View>
          ) : null}

          <TipoEntregaSelector label="Tipo" value={tipo} onChange={setTipo} />
          <TextField label="Descrição (opcional)" value={descricao} onChangeText={setDescricao} placeholder="Ex.: Caixa da Amazon" />
          <TextField label="Observações (opcional)" value={observacoes} onChangeText={setObservacoes} placeholder="Ex.: item frágil" />

          <PrimaryButton
            label="Registrar entrega"
            onPress={salvar}
            loading={salvando}
            disabled={!unidadeId || !moradorDestinatarioId || !tipo}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : semUnidadesCadastradas ? (
        <Text style={styles.avisoVazio}>Cadastre uma unidade na propriedade antes de registrar uma entrega.</Text>
      ) : (
        <PrimaryButton label="Registrar entrega" variant="secondary" onPress={abrirNovo} />
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
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
  },
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
  form: { gap: spacing.sm },
  selectorBlock: { marginBottom: spacing.lg },
  label: { color: colors.sub, fontSize: fontSize.meta, fontWeight: fontWeight.medium, marginBottom: spacing.xs + 2 },
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
