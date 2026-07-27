import React, { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View, type NativeScrollEvent, type NativeSyntheticEvent } from 'react-native';
import Animated, { FadeInDown, FadeOutUp } from 'react-native-reanimated';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ArrowUp, ChevronLeft, Inbox } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import { useRealtimeEvento } from '../../realtime/RealtimeContext';
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
/** Sprint 18 (ADR 0022, Regra 4 — Política de Cache) — Timeline guarda no máximo 50 eventos em memória (FIFO). */
const CACHE_MAXIMO_EVENTOS = 50;
/** Selo "Novo" some depois desse tempo, mesmo sem o usuário rolar a lista. */
const DURACAO_SELO_NOVO_MS = 5000;
/** Abaixo desse deslocamento de scroll, consideramos que o usuário está "no topo" da lista. */
const LIMIAR_TOPO_PX = 24;

/**
 * Orquestrador: busca eventos paginados e decide qual estado renderizar
 * (skeleton/vazio/erro/conteúdo). Paginação via scroll infinito (onEndReached).
 *
 * Sprint 18 (ADR 0022, Fase 2 — Timeline Realtime, Regra 2 — Scroll Preservado):
 * um evento novo (via SignalR) nunca puxa o scroll do usuário. Se ele está no
 * topo, o evento entra direto com animação; se rolou para baixo, o evento fica
 * "pendente" (banner "Ver novos") até ele voltar ao topo ou puxar para
 * atualizar. Só ativa a inserção ao vivo quando não há busca de texto ativa —
 * sem outra fonte de verdade sobre se o evento bateria no filtro de texto ou
 * não, a escolha mais segura é não fingir que ele passaria no filtro.
 */
export function EventosScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const { ultimoEvento } = useRealtimeEvento();

  const [itens, setItens] = useState<EventoResponse[]>([]);
  const [pendentes, setPendentes] = useState<EventoResponse[]>([]);
  const [destaqueNovoId, setDestaqueNovoId] = useState<string | null>(null);
  const [paginaAtual, setPaginaAtual] = useState(1);
  const [totalPaginas, setTotalPaginas] = useState(0);
  const [loading, setLoading] = useState(true);
  const [carregandoMais, setCarregandoMais] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [periodo, setPeriodo] = useState<PeriodoFiltro>('30dias');
  const [busca, setBusca] = useState('');

  const listaRef = useRef<FlatList<EventoResponse>>(null);
  const noTopoRef = useRef(true);
  const seloTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

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

        // Sprint 18 — pull-to-refresh reconcilia com o servidor: os "pendentes"
        // já vieram de volta (ou não, se o filtro os excluiu) dentro de `data.itens`.
        if (modo === 'refresh') {
          setPendentes([]);
        }
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

    setPendentes([]);
    const timeout = setTimeout(() => buscarPagina(1, 'inicial'), DEBOUNCE_BUSCA_MS);
    return () => clearTimeout(timeout);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [periodo, busca]);

  const marcarDestaqueTemporario = useCallback((eventoId: string) => {
    if (seloTimeoutRef.current) {
      clearTimeout(seloTimeoutRef.current);
    }
    setDestaqueNovoId(eventoId);
    seloTimeoutRef.current = setTimeout(() => setDestaqueNovoId(null), DURACAO_SELO_NOVO_MS);
  }, []);

  // Sprint 18 (ADR 0022, Fase 2) — evento novo em tempo real: insere direto no
  // topo se o usuário já está lá; senão, guarda em `pendentes` sem tocar no
  // scroll (Regra 2). Só ativa com busca de texto vazia (ver nota da função).
  const paginaAtualRef = useRef(paginaAtual);
  useEffect(() => {
    paginaAtualRef.current = paginaAtual;
  }, [paginaAtual]);

  useEffect(() => {
    if (!ultimoEvento || !selectedProperty || ultimoEvento.propriedadeId !== selectedProperty.id) {
      return;
    }

    if (busca.trim() || paginaAtualRef.current !== 1) {
      return;
    }

    const evento = ultimoEvento.evento;

    setItens((atual) => {
      if (atual.some((item) => item.id === evento.id)) {
        return atual;
      }

      if (!noTopoRef.current) {
        setPendentes((fila) => (fila.some((item) => item.id === evento.id) ? fila : [evento, ...fila]));
        return atual;
      }

      marcarDestaqueTemporario(evento.id);
      return [evento, ...atual].slice(0, CACHE_MAXIMO_EVENTOS);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ultimoEvento, selectedProperty]);

  useEffect(() => () => {
    if (seloTimeoutRef.current) {
      clearTimeout(seloTimeoutRef.current);
    }
  }, []);

  const handleScroll = useCallback((evento: NativeSyntheticEvent<NativeScrollEvent>) => {
    const noTopo = evento.nativeEvent.contentOffset.y <= LIMIAR_TOPO_PX;
    if (noTopo !== noTopoRef.current) {
      noTopoRef.current = noTopo;
    }
    if (noTopo && destaqueNovoId) {
      // Rolar de volta ao topo também "confirma" o evento novo, removendo o selo mais cedo.
      setDestaqueNovoId(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const verNovos = useCallback(() => {
    setItens((atual) => {
      const mesclados = [...pendentes, ...atual].slice(0, CACHE_MAXIMO_EVENTOS);
      return mesclados;
    });
    setPendentes([]);
    listaRef.current?.scrollToOffset({ offset: 0, animated: true });
  }, [pendentes]);

  const handleEndReached = () => {
    if (!loading && !carregandoMais && paginaAtual < totalPaginas) {
      buscarPagina(paginaAtual + 1, 'mais');
    }
  };

  const handleRefresh = () => {
    setDestaqueNovoId(null);
    buscarPagina(1, 'refresh');
  };

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

        {pendentes.length > 0 ? (
          <Animated.View entering={FadeInDown.duration(220)} exiting={FadeOutUp.duration(220)}>
            <Pressable onPress={verNovos} style={styles.bannerNovos} accessibilityRole="button">
              <ArrowUp size={14} color={colors.bg} />
              <Text style={styles.bannerNovosTexto}>
                {pendentes.length === 1 ? '1 novo evento' : `${pendentes.length} novos eventos`} · Ver novos
              </Text>
            </Pressable>
          </Animated.View>
        ) : null}

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
            ref={listaRef}
            data={itens}
            keyExtractor={(item) => item.id}
            renderItem={({ item }) => <ItemEvento evento={item} destaqueNovo={item.id === destaqueNovoId} />}
            onScroll={handleScroll}
            scrollEventThrottle={100}
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
  bannerNovos: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.sm,
    borderRadius: radius.pill,
    backgroundColor: colors.safe,
    marginBottom: spacing.md,
  },
  bannerNovosTexto: { color: colors.bg, fontSize: fontSize.tiny, fontWeight: fontWeight.bold },
});
