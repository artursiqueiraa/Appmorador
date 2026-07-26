import React, { useCallback, useEffect, useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, ShieldCheck } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import { useRealtime } from '../../realtime/RealtimeContext';
import type { SnapshotOperacionalResponse } from '../../api/types';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import { corEstadoOperacional, emojiEstadoOperacional, rotuloEstadoOperacional } from '../../utils/estadoOperacional';
import { rotuloFabricanteEquipamento } from '../../components/FabricanteEquipamentoSelector';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type SaudePropriedadeRouteProp = RouteProp<RootStackParamList, 'SaudePropriedade'>;

/**
 * Sprint 13 — Camada Operacional Unificada (ADR 0016). Drill-down do Snapshot
 * Operacional: mostra a classificação individual de cada equipamento que compõe a
 * saúde consolidada da Propriedade — mesmo Snapshot da Central Operacional, nunca
 * uma consulta própria a Providers.
 */
export function SaudePropriedadeScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<SaudePropriedadeRouteProp>();
  const { propriedadeId, nomePropriedade } = params;
  const { ultimoSnapshot } = useRealtime();

  const [snapshot, setSnapshot] = useState<SnapshotOperacionalResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const dados = await api.get<SnapshotOperacionalResponse>(`/api/properties/${propriedadeId}/operacional/snapshot`);
      setSnapshot(dados);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar a saúde da propriedade.');
    } finally {
      setLoading(false);
    }
  }, [propriedadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  // Sprint 14 (ADR 0017) — mesma atualização automática da Central Operacional.
  useEffect(() => {
    if (ultimoSnapshot && ultimoSnapshot.propriedadeId === propriedadeId) {
      setSnapshot(ultimoSnapshot.snapshot);
    }
  }, [ultimoSnapshot, propriedadeId]);

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Saúde da propriedade</Text>
          <Text style={styles.subtitle}>{nomePropriedade}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {loading || !snapshot ? (
        <View style={{ gap: spacing.sm }}>
          <Skeleton height={64} radius={radius.lg} />
          <Skeleton height={64} radius={radius.lg} />
        </View>
      ) : (
        <>
          <View style={[styles.resumoCard, { borderColor: corEstadoOperacional(snapshot.saude) }]}>
            <Text style={styles.resumoEmoji}>{emojiEstadoOperacional(snapshot.saude)}</Text>
            <Text style={[styles.resumoTexto, { color: corEstadoOperacional(snapshot.saude) }]}>
              {rotuloEstadoOperacional(snapshot.saude)}
            </Text>
          </View>

          <FlatList
            data={snapshot.equipamentos}
            keyExtractor={(item) => item.equipamentoId}
            contentContainerStyle={styles.list}
            refreshing={loading}
            onRefresh={carregar}
            renderItem={({ item }) => (
              <View style={styles.card}>
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>{item.nome}</Text>
                  <Text style={styles.cardSubtitle}>{rotuloFabricanteEquipamento(item.fabricante)}</Text>
                </View>
                <View style={styles.estadoBadge}>
                  <Text style={styles.estadoEmoji}>{emojiEstadoOperacional(item.estado)}</Text>
                  <Text style={[styles.estadoTexto, { color: corEstadoOperacional(item.estado) }]}>
                    {rotuloEstadoOperacional(item.estado)}
                  </Text>
                </View>
              </View>
            )}
            ListEmptyComponent={
              <EstadoVazio
                icon={ShieldCheck}
                titulo="Nenhum equipamento cadastrado"
                descricao="Cadastre um equipamento ou uma central JFL para acompanhar a saúde da propriedade."
                cta={{ label: 'Ir para Minha Propriedade', onPress: () => navigation.navigate('MinhaPropriedade') }}
              />
            }
          />
        </>
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
  resumoCard: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    padding: spacing.md,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 2,
    marginBottom: spacing.lg,
  },
  resumoEmoji: { fontSize: 24 },
  resumoTexto: { fontSize: fontSize.section, fontWeight: fontWeight.bold },
  list: { paddingBottom: spacing.lg, gap: spacing.sm },
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
  },
  cardTextWrap: { flex: 1 },
  cardTitle: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  cardSubtitle: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  estadoBadge: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs },
  estadoEmoji: { fontSize: 16 },
  estadoTexto: { fontSize: fontSize.label, fontWeight: fontWeight.medium },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
