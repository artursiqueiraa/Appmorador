import React, { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, Inbox } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import { useRealtime } from '../../realtime/RealtimeContext';
import { api, ApiError } from '../../api/client';
import type { EventoResponse, EventosPaginadosResponse } from '../../api/types';
import type { RootStackParamList } from '../../navigation/types';
import { EstadoVazio } from '../../components/EstadoVazio';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';
import { ItemEvento } from './ItemEvento';
import { FiltrosEventos, periodoParaIntervaloUtc, type PeriodoFiltro } from './FiltrosEventos';
import { SkeletonEventos } from './SkeletonEventos';

const TAMANHO_PAGINA = 20;
const DEBOUNCE_BUSCA_MS = 350;

/**
 * Orquestrador: busca eventos paginados e decide qual estado renderizar
 * (skeleton/vazio/erro/conteúdo). Paginação via scroll infinito (onEndReached).
 */
export function EventosScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const { ultimoEvento } = useRealtime();

  const [itens, setItens] = useState<EventoResponse[]>([]);
  const [paginaAtual, setPaginaAtual] = useState(1);
  const [totalPaginas, setTotalPaginas] = useState(0);
  const [loading, setLoading] = useState(true);
  const [carregandoMais, setCarregandoMais] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [periodo, setPeriodo] = useState<PeriodoFiltro>('30dias');
  const [busca, setBusca] = useState('');

  const buscarPagina = useCallback(
    async (pagina: number, modo: 'inicial' | 'refresh' | 'mais') => {
      if (!selectedProperty) {
        return;
      }

      if (modo === 'inicial') setLoading(true);
      if (modo === 'refresh') setRefreshing(true);
      if (modo === 'mais') setCarregandoMais(true);
      setError(null);

      try {
        const { desdeUtc } = periodoParaIntervaloUtc(periodo);
        const params = new URLSearchParams({ pagina: String(pagina), tamanhoPagina: String(TAMANHO_PAGINA) });
        if (busca.trim()) params.set('busca', busca.trim());
        if (desdeUtc) params.set('desdeUtc', desdeUtc);

        const data = await api.get<EventosPaginadosResponse>(
          `/api/properties/${selectedProperty.id}/eventos?${params.toString()}`,
        );

        setItens((atual) => (modo === 'mais' ? [...atual, ...data.itens] : data.itens));
        setPaginaAtual(data.paginaAtual);
        setTotalPaginas(data.totalPaginas);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : 'Não foi possível carregar os eventos.');
      } finally {
        setLoading(false);
        setRefreshing(false);
        setCarregandoMais(false);
      }
    },
    [selectedProperty, periodo, busca],
  );

  // Muda período ou busca -> reinicia da primeira página.
  const primeiraExecucao = useRef(true);
  useEffect(() => {
    if (primeiraExecucao.current) {
      primeiraExecucao.current = false;
      buscarPagina(1, 'inicial');
      return;
    }

    const timeout = setTimeout(() => buscarPagina(1, 'inicial'), DEBOUNCE_BUSCA_MS);
    return () => clearTimeout(timeout);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [periodo, busca]);

  // Sprint 14 (ADR 0017) — um novo evento operacional (ex.: alarme disparado) refaz
  // só a primeira página, sem perturbar quem já rolou a lista para trás. O payload
  // do SignalR é só um sinal ("algo novo aconteceu") — a Central de Eventos via GET
  // continua a fonte de verdade para o conteúdo real, filtros inclusive.
  const paginaAtualRef = useRef(paginaAtual);
  useEffect(() => {
    paginaAtualRef.current = paginaAtual;
  }, [paginaAtual]);

  useEffect(() => {
    if (!ultimoEvento || !selectedProperty || ultimoEvento.propriedadeId !== selectedProperty.id) {
      return;
    }

    if (paginaAtualRef.current === 1) {
      buscarPagina(1, 'refresh');
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ultimoEvento]);

  const handleEndReached = () => {
    if (!loading && !carregandoMais && paginaAtual < totalPaginas) {
      buscarPagina(paginaAtual + 1, 'mais');
    }
  };

  const handleRefresh = () => buscarPagina(1, 'refresh');

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Pressable
          onPress={() => navigation.goBack()}
          style={styles.iconBtn}
          accessibilityRole="button"
          accessibilityLabel="Voltar"
        >
          <ChevronLeft size={20} color={colors.sub} />
        </Pressable>
        <Text style={styles.titulo}>Eventos</Text>
        <View style={styles.spacer} />
      </View>

      <View style={styles.content}>
        <FiltrosEventos periodo={periodo} onChangePeriodo={setPeriodo} busca={busca} onChangeBusca={setBusca} />

        {error ? <Text style={styles.error}>{error}</Text> : null}

        {loading && itens.length === 0 ? (
          <SkeletonEventos />
        ) : itens.length === 0 ? (
          <EstadoVazio
            icon={Inbox}
            titulo="Nenhum evento por aqui"
            descricao="Quando algo acontecer na sua propriedade, você vai ver aqui."
          />
        ) : (
          <FlatList
            data={itens}
            keyExtractor={(item) => item.id}
            renderItem={({ item }) => <ItemEvento evento={item} />}
            onEndReached={handleEndReached}
            onEndReachedThreshold={0.4}
            refreshing={refreshing}
            onRefresh={handleRefresh}
            contentContainerStyle={styles.lista}
            ListFooterComponent={carregandoMais ? <ActivityIndicator color={colors.safe} style={styles.rodape} /> : null}
          />
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.xl,
    paddingTop: spacing.xl,
    paddingBottom: spacing.md,
  },
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
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold },
  spacer: { width: 38, height: 38 },
  content: { flex: 1, paddingHorizontal: spacing.xl },
  lista: { paddingBottom: spacing.xxl },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
  rodape: { marginVertical: spacing.md },
});