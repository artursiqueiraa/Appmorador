import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Bell, MapPin } from 'lucide-react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useAuth } from '../../auth/AuthContext';
import { usePermissao } from '../../auth/usePermissao';
import { useRealtimeEvento } from '../../realtime/RealtimeContext';
import { api } from '../../api/client';
import type { CameraResponse, EventosPaginadosResponse } from '../../api/types';
import type { RootStackParamList } from '../../navigation/types';
import { IndicadorConexaoRealtime } from '../../components/IndicadorConexaoRealtime';
import { SectionHeader } from '../../components/SectionHeader';
import { EstadoVazio } from '../../components/EstadoVazio';
import { ListaPortoes } from '../../acessos/ListaPortoes';
import { BotoesArme } from '../../alarme/BotoesArme';
import { HeroStatus } from '../../alarme/HeroStatus';
import { useCentralAlarme } from '../../alarme/useCentralAlarme';
import { CameraMini } from '../../cameras/CameraMini';
import { useLayoutDesktop } from '../../layout/useLayoutDesktop';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import { iconeEvento } from '../../utils/iconeEvento';
import { BotaoPanico } from '../../alertas/BotaoPanico';
import { podeOperar } from '../../pessoas/papeis';
import { MarcaAkteck } from '../../components/marca/MarcaAkteck';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

function saudacao(): string {
  const h = new Date().getHours();
  return h < 12 ? 'Bom dia' : h < 18 ? 'Boa tarde' : 'Boa noite';
}

/** Linha de "Atividade recente" do protótipo: ícone em quadrado, título e "detalhe, há X". */
function LinhaAtividade({ titulo, descricao, ocorridoEmUtc, destaque }: { titulo: string; descricao?: string | null; ocorridoEmUtc: string; destaque: boolean }) {
  const { icone: Icone, tom } = iconeEvento(titulo, destaque);
  const cor = tom === 'perigo' ? colors.danger : tom === 'atencao' ? colors.warn : tom === 'marca' ? colors.safe : colors.accent;
  return (
    <View style={styles.atividade}>
      <View style={styles.atividadeIcone}>
        <Icone size={17} color={cor} />
      </View>
      <View style={styles.atividadeTexto}>
        <Text style={styles.atividadeTitulo} numberOfLines={1}>
          {titulo}
        </Text>
        <Text style={styles.atividadeMeta} numberOfLines={1}>
          {[descricao, formatRelativeTime(ocorridoEmUtc)].filter(Boolean).join(', ')}
        </Text>
      </View>
    </View>
  );
}

/**
 * Akteck Seg (ADR 0038) — Início do protótipo original: saudação e local, cartão de status com
 * o anel pulsando, os três botões de arme (agora com comando real na central), abrir portão,
 * câmeras e atividade recente. Tudo com dados reais; atividade ao vivo via SignalR.
 */
export function HomeScreen() {
  const desktop = useLayoutDesktop();
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { user, selectedProperty, trocarPropriedade } = useAuth();
  const { temFeature } = usePermissao();
  const { ultimoEvento } = useRealtimeEvento();
  const alarme = useCentralAlarme();
  // Sprint 24.5 — portões vêm dos pontos de acesso com acionador (mesma fonte da tela Portões e portas).
  const [qtdPortoes, setQtdPortoes] = useState(0);
  const [versaoPortoes, setVersaoPortoes] = useState(0);

  const [atividades, setAtividades] = useState<EventosPaginadosResponse['itens']>([]);
  const [eventosHoje, setEventosHoje] = useState(0);
  const [cameras, setCameras] = useState<CameraResponse[]>([]);
  const [carregando, setCarregando] = useState(true);
  const temCameras = temFeature('Cameras');

  const carregar = useCallback(async () => {
    if (!selectedProperty) return;
    setCarregando(true);
    try {
      const [eventos, listaCameras] = await Promise.all([
        api.get<EventosPaginadosResponse>(`/api/properties/${selectedProperty.id}/eventos?pagina=1&tamanhoPagina=4`),
        temCameras ? api.get<CameraResponse[]>(`/api/properties/${selectedProperty.id}/cameras`).catch(() => []) : Promise.resolve([]),
      ]);
      setAtividades(eventos.itens);
      const hoje = new Date().toDateString();
      setEventosHoje(eventos.itens.filter((e) => new Date(e.ocorridoEmUtc).toDateString() === hoje).length);
      setCameras(listaCameras);
    } catch {
      setAtividades([]);
    } finally {
      setCarregando(false);
    }
  }, [selectedProperty, temCameras]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  // Troca de propriedade: nada da casa anterior aparece nem por um instante.
  const propriedadeAnteriorRef = useRef(selectedProperty?.id ?? null);
  useEffect(() => {
    const novo = selectedProperty?.id ?? null;
    if (propriedadeAnteriorRef.current !== novo) {
      propriedadeAnteriorRef.current = novo;
      setAtividades([]);
      setCameras([]);
    }
  }, [selectedProperty?.id]);

  // Sprint 18 (ADR 0022) — evento novo entra ao vivo. Sprint 24: o alerta em tela cheia agora vem do
  // alerta crítico real (TelaAlertaCritico, montada uma vez no app), não deste evento.
  useEffect(() => {
    if (!ultimoEvento || !selectedProperty || ultimoEvento.propriedadeId !== selectedProperty.id) return;
    setAtividades((atual) => (atual.some((i) => i.id === ultimoEvento.evento.id) ? atual : [ultimoEvento.evento, ...atual].slice(0, 4)));
    setEventosHoje((n) => n + 1);
  }, [ultimoEvento, selectedProperty]);

  if (!selectedProperty) return null;

  const detalheHero =
    alarme.estado === 'total' || alarme.estado === 'parcial'
      ? `${alarme.estado === 'total' ? 'Todas as áreas ativas' : 'Perímetro ativo, interior liberado'}${
          alarme.atualizadoEm ? `, desde ${alarme.atualizadoEm.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}` : ''
        }`
      : null;

  const status = (
    <>
      <HeroStatus
        estado={alarme.estado}
        detalhe={detalheHero}
        onPress={() => (alarme.estado === 'semCentral' ? navigation.navigate('Onboarding', { propriedadeId: selectedProperty.id }) : navigation.navigate('MainTabs', { screen: 'Alarme' }))}
      />
      {alarme.estado !== 'semCentral' ? (
        <View style={styles.botoes}>
          <BotoesArme estado={alarme.estado} enviando={alarme.enviando} disponivel={alarme.particoes.length > 0 && podeOperar(selectedProperty.perfil)} onComando={alarme.comandar} />
        </View>
      ) : null}
      {alarme.erro ? <Text style={styles.erro}>{alarme.erro}</Text> : null}

      {qtdPortoes > 0 ? (
        <SectionHeader
          title="Portões"
          actionLabel="Ver todos"
          onPressAction={() => navigation.navigate('PontosAcesso', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome })}
        />
      ) : null}
      <ListaPortoes key={versaoPortoes} propriedadeId={selectedProperty.id} aoCarregar={setQtdPortoes} />

      {podeOperar(selectedProperty.perfil) ? (
        <View style={styles.panico}>
          <BotaoPanico propriedadeId={selectedProperty.id} />
        </View>
      ) : null}
    </>
  );

  const lateral = (
    <>
      {temCameras && cameras.length > 0 ? (
        <>
          <SectionHeader title="Câmeras" actionLabel="Ver todas" onPressAction={() => navigation.navigate('MainTabs', { screen: 'Cameras' })} />
          <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.cameras}>
            {cameras.map((c) => (
              <CameraMini key={c.id} camera={c} onPress={() => navigation.navigate('DetalheCamera', { cameraId: c.id, nomeCamera: c.nome })} />
            ))}
          </ScrollView>
        </>
      ) : null}

      <SectionHeader title="Atividade recente" actionLabel="Histórico" onPressAction={() => navigation.navigate('Eventos')} />
      {atividades.length === 0 && !carregando ? (
        <EstadoVazio icon={Bell} titulo="Nenhuma atividade ainda" descricao="Quando alguém entrar, sair ou algo acontecer na sua casa, aparece aqui." />
      ) : (
        <View>
          {atividades.map((a) => (
            <LinhaAtividade key={a.id} titulo={a.titulo} descricao={a.descricao} ocorridoEmUtc={a.ocorridoEmUtc} destaque={a.destaque} />
          ))}
        </View>
      )}

    </>
  );

  return (
    <View style={styles.tela}>
      <ScrollView
        style={styles.tela}
        contentContainerStyle={[styles.content, desktop && styles.contentDesktop]}
        refreshControl={
          <RefreshControl
            refreshing={carregando}
            onRefresh={() => {
              carregar();
              alarme.lerStatus();
              setVersaoPortoes((v) => v + 1);
            }}
            tintColor={colors.safe}
          />
        }
      >
        {/* Marca no topo do Início (como na versão anterior), sino à direita. */}
        <View style={styles.topo}>
          <View style={styles.topoLado} />
          <MarcaAkteck variante="empilhada" tamanho={24} />
          <View style={[styles.topoLado, styles.topoDireita]}>
            <Pressable onPress={() => navigation.navigate('Eventos')} style={styles.sino} accessibilityRole="button" accessibilityLabel="Atividades">
              <Bell size={iconSize.md - 2} color={colors.sub} />
              {eventosHoje > 0 ? <View style={styles.sinoPonto} /> : null}
            </Pressable>
          </View>
        </View>
        <View style={styles.cabecalho}>
          <View style={styles.cabecalhoTexto}>
            <Text style={styles.ola}>
              {saudacao()}, {user?.nome?.split(' ')[0] ?? ''}
            </Text>
            <Pressable onPress={trocarPropriedade} style={styles.local} accessibilityRole="button" accessibilityLabel={`${selectedProperty.nome}. Trocar propriedade`}>
              <MapPin size={13} color={colors.mute} />
              <Text style={styles.localTexto} numberOfLines={1}>
                {selectedProperty.nome}
              </Text>
            </Pressable>
          </View>
        </View>

        <IndicadorConexaoRealtime />

        {desktop ? (
          <View style={styles.duasColunas}>
            <View style={styles.colunaPrincipal}>{status}</View>
            <View style={styles.colunaLateral}>{lateral}</View>
          </View>
        ) : (
          <>
            {status}
            {lateral}
          </>
        )}
      </ScrollView>

    </View>
  );
}

const styles = StyleSheet.create({
  panico: { marginTop: spacing.lg },
  tela: { flex: 1, backgroundColor: colors.bg },
  content: { paddingHorizontal: 18, paddingBottom: spacing.xxl },
  contentDesktop: { paddingHorizontal: spacing.xxl, paddingTop: spacing.lg },
  topo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingTop: spacing.md, paddingBottom: spacing.md },
  topoLado: { width: 40 },
  topoDireita: { alignItems: 'flex-end' },
  cabecalho: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingBottom: 18 },
  cabecalhoTexto: { flex: 1, minWidth: 0 },
  ola: { color: colors.sub, fontSize: fontSize.secondary },
  local: { flexDirection: 'row', alignItems: 'center', gap: 5, marginTop: 3, alignSelf: 'flex-start', minHeight: 24 },
  localTexto: { color: colors.text, fontSize: fontSize.secondary, fontWeight: fontWeight.bold },
  sino: {
    width: 40,
    height: 40,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  sinoPonto: { position: 'absolute', top: 9, right: 9, width: 8, height: 8, borderRadius: 4, backgroundColor: colors.danger, borderWidth: 2, borderColor: colors.surface },
  botoes: { marginTop: spacing.lg },
  erro: { color: colors.danger, fontSize: fontSize.secondary, marginTop: spacing.sm, textAlign: 'center' },
  portoes: { gap: spacing.sm },
  cameras: { gap: 10, paddingBottom: 4 },
  atividade: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, paddingVertical: 11, borderBottomWidth: 1, borderBottomColor: colors.lineSoft },
  atividadeIcone: { width: 38, height: 38, borderRadius: 11, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line, alignItems: 'center', justifyContent: 'center' },
  atividadeTexto: { flex: 1, minWidth: 0 },
  atividadeTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  atividadeMeta: { color: colors.mute, fontSize: fontSize.tiny + 0.5, marginTop: 2 },
  duasColunas: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.xxl },
  colunaPrincipal: { flex: 1, minWidth: 0 },
  colunaLateral: { flex: 1, minWidth: 0 },
});
