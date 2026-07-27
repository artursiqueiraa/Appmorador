import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, ChevronRight, Clock, ShieldAlert, Wifi, WifiOff } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import { useRealtimeSnapshot } from '../../realtime/RealtimeContext';
import type { SnapshotOperacionalResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Skeleton } from '../../components/Skeleton';
import { formatRelativeTime } from '../../utils/formatRelativeTime';
import { corEstadoOperacional, rotuloEstadoOperacional } from '../../utils/estadoOperacional';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

type CentralOperacionalRouteProp = RouteProp<RootStackParamList, 'CentralOperacional'>;

/**
 * Sprint 13 — Camada Operacional Unificada (ADR 0016). Consome exclusivamente o
 * Snapshot Operacional (nunca IControlIdProvider/IJflProvider diretamente) — a
 * atualização manual é a única forma de recalculá-lo, nunca automática/polling.
 */
export function CentralOperacionalScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<CentralOperacionalRouteProp>();
  const { propriedadeId, nomePropriedade } = params;
  const { ultimoSnapshot } = useRealtimeSnapshot();

  const [snapshot, setSnapshot] = useState<SnapshotOperacionalResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [atualizando, setAtualizando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const dados = await api.get<SnapshotOperacionalResponse>(`/api/properties/${propriedadeId}/operacional/snapshot`);
      setSnapshot(dados);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar o snapshot operacional.');
    } finally {
      setLoading(false);
    }
  }, [propriedadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  // Sprint 14 (ADR 0017) — atualização automática via SignalR: o payload já é o
  // Snapshot completo, substitui direto sem um novo GET. "Atualizar snapshot" abaixo
  // continua funcionando como fallback manual independente da conexão em tempo real.
  useEffect(() => {
    if (ultimoSnapshot && ultimoSnapshot.propriedadeId === propriedadeId) {
      setSnapshot(ultimoSnapshot.snapshot);
    }
  }, [ultimoSnapshot, propriedadeId]);

  const atualizar = async () => {
    setAtualizando(true);
    setError(null);
    try {
      const dados = await api.post<SnapshotOperacionalResponse>(`/api/properties/${propriedadeId}/operacional/snapshot/atualizar`);
      setSnapshot(dados);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível atualizar o snapshot.');
    } finally {
      setAtualizando(false);
    }
  };

  if (loading || !snapshot) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
            <ChevronLeft size={20} color={colors.text} />
          </Pressable>
          <Text style={styles.title}>Central Operacional</Text>
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
          <Text style={styles.title}>Central Operacional</Text>
          <Text style={styles.subtitle}>{nomePropriedade}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <View style={[styles.saudeCard, { borderColor: corEstadoOperacional(snapshot.saude) }]}>
        <Text style={[styles.saudeLabel, { color: corEstadoOperacional(snapshot.saude) }]}>
          {rotuloEstadoOperacional(snapshot.saude)}
        </Text>
        <Text style={styles.saudeGerado}>
          Gerado {formatRelativeTime(snapshot.geradoEmUtc)}
        </Text>
      </View>

      <View style={styles.grid}>
        <View style={styles.item}>
          <View style={styles.iconWrap}>
            <Wifi size={iconSize.md} color={colors.accent} />
          </View>
          <Text style={styles.itemValor}>{snapshot.quantidadeEquipamentosOnline}</Text>
          <Text style={styles.itemRotulo}>Online</Text>
        </View>
        <View style={styles.item}>
          <View style={styles.iconWrap}>
            <WifiOff size={iconSize.md} color={colors.accent} />
          </View>
          <Text style={styles.itemValor}>{snapshot.quantidadeEquipamentosOffline}</Text>
          <Text style={styles.itemRotulo}>Offline</Text>
        </View>
        <View style={styles.item}>
          <View style={styles.iconWrap}>
            <Clock size={iconSize.md} color={colors.accent} />
          </View>
          <Text style={styles.itemValor}>{snapshot.quantidadeEventosHoje}</Text>
          <Text style={styles.itemRotulo}>Eventos hoje</Text>
        </View>
        <View style={styles.item}>
          <View style={styles.iconWrap}>
            <ShieldAlert size={iconSize.md} color={colors.accent} />
          </View>
          <Text style={styles.itemValor}>{snapshot.quantidadeAlarmesAtivos}</Text>
          <Text style={styles.itemRotulo}>Alarmes ativos</Text>
        </View>
      </View>

      <Text style={styles.campo}>
        Última comunicação: {snapshot.ultimaComunicacaoUtc ? formatRelativeTime(snapshot.ultimaComunicacaoUtc) : 'Nunca'}
      </Text>

      <View style={styles.acoes}>
        <PrimaryButton
          label="Atualizar snapshot"
          onPress={atualizar}
          loading={atualizando}
        />
        <Pressable
          style={styles.linkLinha}
          onPress={() => navigation.navigate('SaudePropriedade', { propriedadeId, nomePropriedade })}
        >
          <Text style={styles.linkTexto}>Ver saúde por equipamento</Text>
          <ChevronRight size={16} color={colors.mute} />
        </Pressable>
        <Pressable style={styles.linkLinha} onPress={() => navigation.navigate('Eventos')}>
          <Text style={styles.linkTexto}>Ver Central de Eventos</Text>
          <ChevronRight size={16} color={colors.mute} />
        </Pressable>
      </View>
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
  saudeCard: {
    padding: spacing.lg,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 2,
    alignItems: 'center',
    marginBottom: spacing.lg,
    gap: spacing.xs,
  },
  saudeLabel: { fontSize: fontSize.hero, fontWeight: fontWeight.bold },
  saudeGerado: { color: colors.mute, fontSize: fontSize.tiny },
  grid: { flexDirection: 'row', flexWrap: 'wrap', rowGap: spacing.md, marginBottom: spacing.lg },
  item: { alignItems: 'center', gap: spacing.xs, width: '25%' },
  iconWrap: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface2,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  itemValor: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  itemRotulo: { color: colors.mute, fontSize: fontSize.label, textAlign: 'center' },
  campo: { color: colors.sub, fontSize: fontSize.secondary, marginBottom: spacing.lg },
  acoes: { gap: spacing.sm },
  linkLinha: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  linkTexto: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
