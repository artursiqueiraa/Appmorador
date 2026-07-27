import React, { useCallback, useEffect, useRef, useState } from 'react';
import { RefreshControl, ScrollView, StyleSheet, Text } from 'react-native';
import Animated, { FadeIn } from 'react-native-reanimated';
import {
  DoorOpen,
  Lock,
  MoonStar,
  QrCode,
  ScanFace,
  Unlock,
} from 'lucide-react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useAuth } from '../../auth/AuthContext';
import { useRealtimeEvento, useRealtimeSnapshot } from '../../realtime/RealtimeContext';
import { api, ApiError } from '../../api/client';
import type { DashboardResponse, EventosPaginadosResponse } from '../../api/types';
import type { RootStackParamList } from '../../navigation/types';
import { ProfileHeader } from '../../components/ProfileHeader';
import { IndicadorConexaoRealtime } from '../../components/IndicadorConexaoRealtime';
import { HeroCard, type Conectividade, type HeroStatus } from '../../components/HeroCard';
import { QuickAction } from '../../components/QuickAction';
import { SectionHeader } from '../../components/SectionHeader';
import { CameraCard } from '../../components/CameraCard';
import { ActivityCard } from '../../components/ActivityCard';
import { EstadoVazio } from '../../components/EstadoVazio';
import { DemoButton } from '../../components/DemoButton';
import { PropertyCard } from '../../components/PropertyCard';
import { AlertaDisparo } from './AlertaDisparo';
import { SkeletonDashboard } from '../dashboard/SkeletonDashboard';
import { ServicoFeedbackTatil } from '../../services/ServicoFeedbackTatil';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import { colors, motion, spacing } from '../../theme/theme';

type ModoArme = 'protegido' | 'noturno' | 'desarmado';

/**
 * Sprint 16 (ADR 0019, UX001) — substitui DashboardScreen. Máximo 5 blocos
 * principais (HeroCard, Câmeras, Atividade, Configuração pendente, Demo) — todo
 * conteúdo adicional fica em telas próprias (Acessos/Câmeras/Ajustes), nunca
 * empilhado aqui. Botões de arme continuam visuais nesta Sprint (mesma limitação já
 * registrada desde a Sprint 2 — decidir "qual central é a padrão" quando há mais de
 * uma é uma decisão de produto que esta Sprint de UX não resolve, ver DIVIDA_TECNICA).
 */
export function HomeScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { user, selectedProperty } = useAuth();
  const { ultimoSnapshot } = useRealtimeSnapshot();
  const { ultimoEvento } = useRealtimeEvento();

  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null);
  const [atividades, setAtividades] = useState<EventosPaginadosResponse['itens']>([]);
  const [modoArme, setModoArme] = useState<ModoArme>('desarmado');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [alertaVisivel, setAlertaVisivel] = useState(false);

  const carregar = useCallback(async () => {
    if (!selectedProperty) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const [dados, eventos] = await Promise.all([
        api.get<DashboardResponse>(`/api/properties/${selectedProperty.id}/dashboard`),
        api.get<EventosPaginadosResponse>(`/api/properties/${selectedProperty.id}/eventos?pagina=1&tamanhoPagina=3`),
      ]);
      setDashboard(dados);
      setAtividades(eventos.itens);
      setModoArme(dados.quantidadeParticoesArmadas > 0 ? 'protegido' : 'desarmado');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar sua casa agora.');
    } finally {
      setLoading(false);
    }
  }, [selectedProperty]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  // Sprint 18 (ADR 0022, Fase 8 — Troca de Propriedade) — ao trocar de
  // propriedade, o dashboard/atividades da propriedade anterior nunca deve
  // aparecer nem por um instante: descarta o cache local imediatamente (volta
  // ao estado de carregamento sutil) antes do novo GET (disparado por
  // `carregar`, acima) resolver. Não roda na primeira montagem — `carregar` já
  // cobre o carregamento inicial sozinho.
  const propriedadeIdAnteriorRef = useRef<string | null>(selectedProperty?.id ?? null);
  useEffect(() => {
    const novoId = selectedProperty?.id ?? null;
    if (propriedadeIdAnteriorRef.current === novoId) {
      return;
    }
    propriedadeIdAnteriorRef.current = novoId;
    setDashboard(null);
    setAtividades([]);
  }, [selectedProperty?.id]);

  // Sprint 14 (ADR 0017) — atualização automática via SignalR, sem esperar refresh manual.
  useEffect(() => {
    if (!ultimoSnapshot || !selectedProperty || ultimoSnapshot.propriedadeId !== selectedProperty.id) {
      return;
    }

    const { snapshot } = ultimoSnapshot;
    setDashboard((atual) =>
      atual
        ? {
            ...atual,
            saude: snapshot.saude,
            quantidadeEquipamentosOnline: snapshot.quantidadeEquipamentosOnline,
            quantidadeEquipamentosOffline: snapshot.quantidadeEquipamentosOffline,
            quantidadeEventosHoje: snapshot.quantidadeEventosHoje,
            quantidadeAlarmesAtivos: snapshot.quantidadeAlarmesAtivos,
            ultimaAtualizacaoOperacionalUtc: snapshot.geradoEmUtc,
          }
        : atual,
    );
  }, [ultimoSnapshot, selectedProperty]);

  // Um novo evento em tempo real acende o alerta de disparo automaticamente quando
  // é algo grave (destaque=true) — o morador nunca precisa abrir o app para saber.
  useEffect(() => {
    if (ultimoEvento && selectedProperty && ultimoEvento.propriedadeId === selectedProperty.id && ultimoEvento.evento.destaque) {
      setAlertaVisivel(true);
    }
  }, [ultimoEvento, selectedProperty]);

  // Sprint 18 (ADR 0022, Fase 3, Regra 1) — "Atividade recente" também recebe o
  // evento novo ao vivo (sem refetch), tornando o Início uma tela onde o evento já
  // é visível por si só — é por isso que o RealtimeToastBridge não mostra toast
  // quando o morador está no Início.
  useEffect(() => {
    if (!ultimoEvento || !selectedProperty || ultimoEvento.propriedadeId !== selectedProperty.id) {
      return;
    }
    setAtividades((atual) => {
      if (atual.some((item) => item.id === ultimoEvento.evento.id)) {
        return atual;
      }
      return [ultimoEvento.evento, ...atual].slice(0, 3);
    });
  }, [ultimoEvento, selectedProperty]);

  const handleArme = useCallback((modo: ModoArme) => {
    ServicoFeedbackTatil.impactLight();
    setModoArme(modo);
  }, []);

  const armarTotal = useCallback(() => handleArme('protegido'), [handleArme]);
  const armarNoturno = useCallback(() => handleArme('noturno'), [handleArme]);
  const desarmar = useCallback(() => handleArme('desarmado'), [handleArme]);

  if (loading && !dashboard) {
    return <SkeletonDashboard />;
  }

  if (!dashboard || !selectedProperty) {
    return null;
  }

  const semCentral = dashboard.quantidadeEquipamentosOnline + dashboard.quantidadeEquipamentosOffline === 0;
  const { status, titulo, subtitulo } = computarStatusHero(dashboard, modoArme, semCentral);
  const temCameras = dashboard.quantidadeCameras > 0;

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={loading} onRefresh={carregar} tintColor={colors.safe} />}
    >
      <ProfileHeader
        saudacao={`Bom dia, ${user?.nome?.split(' ')[0] ?? ''}`}
        nomePropriedade={dashboard.nome}
        temAtividadeNaoVista={dashboard.quantidadeEventosHoje > 0}
        onPressNotificacoes={() => navigation.navigate('Eventos')}
      />

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Animated.View entering={FadeIn.duration(motion.duration.base)}>
        <IndicadorConexaoRealtime />
        {semCentral ? (
          <PropertyCard
            nome="Complete sua configuração"
            tipo={dashboard.tipo}
            endereco="Adicione uma central para proteger sua casa"
            onPress={() => navigation.navigate('Onboarding', { propriedadeId: selectedProperty.id })}
          />
        ) : (
          <HeroCard status={status} titulo={titulo} subtitulo={subtitulo} conectividade={computarConectividade(dashboard)}>
            <QuickAction icon={Lock} label="Armar total" active={modoArme === 'protegido'} onPress={armarTotal} />
            <QuickAction icon={MoonStar} label="Noturno" active={modoArme === 'noturno'} onPress={armarNoturno} />
            <QuickAction icon={Unlock} label="Desarmar" tone="warn" active={modoArme === 'desarmado'} onPress={desarmar} />
          </HeroCard>
        )}

        {temCameras ? (
          <>
            <SectionHeader title="Câmeras" actionLabel="Ver todas" onPressAction={() => navigation.navigate('MainTabs', { screen: 'Cameras' })} />
            <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.cameraLista}>
              <CameraCard nome="Entrada" onPress={() => navigation.navigate('MainTabs', { screen: 'Cameras' })} />
              <CameraCard nome="Sala" onPress={() => navigation.navigate('MainTabs', { screen: 'Cameras' })} />
            </ScrollView>
          </>
        ) : null}

        <SectionHeader title="Atividade recente" actionLabel="Histórico" onPressAction={() => navigation.navigate('Eventos')} />
        {atividades.length === 0 ? (
          <EstadoVazio
            icon={DoorOpen}
            titulo="Nenhuma atividade ainda"
            descricao="Quando alguém entrar, sair ou algo acontecer na sua casa, você vê aqui."
          />
        ) : (
          atividades.map((item) => (
            <ActivityCard
              key={item.id}
              icon={item.destaque ? ScanFace : QrCode}
              color={item.destaque ? colors.danger : colors.accent}
              title={item.titulo}
              meta={formatarQuando(item.ocorridoEmUtc)}
            />
          ))
        )}

        <DemoButton label="Simular alarme (demonstração)" onPress={() => setAlertaVisivel(true)} />
      </Animated.View>

      {alertaVisivel ? <AlertaDisparo onClose={() => setAlertaVisivel(false)} /> : null}
    </ScrollView>
  );
}

function computarStatusHero(
  dashboard: DashboardResponse,
  modoArme: ModoArme,
  semCentral: boolean,
): { status: HeroStatus; titulo: string; subtitulo: string } {
  const quando = dashboard.ultimaAtualizacaoOperacionalUtc ? formatarQuando(dashboard.ultimaAtualizacaoOperacionalUtc) : 'agora mesmo';

  if (semCentral) {
    return { status: 'desarmado', titulo: 'CONFIGURE SUA PROTEÇÃO', subtitulo: 'Adicione uma central para monitorar sua casa' };
  }

  if (dashboard.quantidadeAlarmesAtivos > 0 || dashboard.saude === 'Critico') {
    return { status: 'atencao', titulo: 'ATENÇÃO', subtitulo: 'Alguma coisa precisa da sua atenção agora' };
  }

  if (modoArme === 'desarmado') {
    return { status: 'desarmado', titulo: 'DESARMADO', subtitulo: 'Sua casa não está protegida no momento' };
  }

  if (modoArme === 'noturno') {
    return { status: 'protegido', titulo: 'MODO NOTURNO', subtitulo: `Perímetro ativo · atualizado ${quando}` };
  }

  return { status: 'protegido', titulo: 'PROTEGIDO', subtitulo: `Sua casa está sendo monitorada · atualizado ${quando}` };
}

/**
 * Sprint 17 (ADR 0020, achado #7) — "Online"/"Offline" cru não diz nada útil para
 * quem não é técnico. Usa só dados já reais e já atualizados via SignalR (Sprint 14):
 * contagem de equipamentos online/offline e `ultimaAtualizacaoOperacionalUtc`.
 */
function computarConectividade(dashboard: DashboardResponse): Conectividade | undefined {
  const totalEquipamentos = dashboard.quantidadeEquipamentosOnline + dashboard.quantidadeEquipamentosOffline;
  if (totalEquipamentos === 0) {
    return undefined;
  }

  if (dashboard.quantidadeEquipamentosOnline === 0) {
    const desde = dashboard.ultimaAtualizacaoOperacionalUtc
      ? new Date(dashboard.ultimaAtualizacaoOperacionalUtc).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
      : null;
    return { estado: 'offline', label: desde ? `Sem comunicação desde ${desde}` : 'Sem comunicação ainda' };
  }

  if (!dashboard.ultimaAtualizacaoOperacionalUtc) {
    return { estado: 'conectado', label: 'Conectado' };
  }

  const minutos = Math.round((Date.now() - new Date(dashboard.ultimaAtualizacaoOperacionalUtc).getTime()) / 60000);
  if (minutos <= 2) {
    return { estado: 'conectado', label: 'Conectado' };
  }

  return { estado: 'atencao', label: `Última comunicação ${formatRelativeTime(dashboard.ultimaAtualizacaoOperacionalUtc)}` };
}

function formatarQuando(isoDate: string): string {
  const diffMin = Math.max(0, Math.round((Date.now() - new Date(isoDate).getTime()) / 60000));
  if (diffMin < 1) return 'agora mesmo';
  if (diffMin < 60) return `há ${diffMin} min`;
  const horas = Math.round(diffMin / 60);
  return `há ${horas} h`;
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl },
  cameraLista: { gap: spacing.sm, paddingBottom: spacing.xs },
  error: { color: colors.danger, fontSize: 13, marginBottom: spacing.md, textAlign: 'center' },
});
