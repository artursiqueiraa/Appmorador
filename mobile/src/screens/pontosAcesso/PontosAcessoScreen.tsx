import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Car, ChevronLeft, DoorOpen, Pencil, Trash2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { PontoAcessoResponse, TipoPontoAcesso } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type PontosAcessoRouteProp = RouteProp<RootStackParamList, 'PontosAcesso'>;

/**
 * Sprint 7 — Controle de Acesso. Mesmo padrão de tela de `UnidadesScreen` (lista +
 * formulário inline). Pontos de Acesso pertencem direto à Propriedade (ex.: Portão
 * Principal, Piscina) — nenhum nível abaixo, então não há navegação ao tocar o card.
 */
export function PontosAcessoScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<PontosAcessoRouteProp>();
  const { propriedadeId, nomePropriedade } = params;

  const [pontos, setPontos] = useState<PontoAcessoResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<PontoAcessoResponse | null>(null);
  const [nome, setNome] = useState('');
  const [tipo, setTipo] = useState<TipoPontoAcesso>('Geral');
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const lista = await api.get<PontoAcessoResponse[]>(`/api/properties/${propriedadeId}/pontos-acesso`);
      setPontos(lista);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar os pontos de acesso.');
    } finally {
      setLoading(false);
    }
  }, [propriedadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const abrirNovo = () => {
    setEditando(null);
    setNome('');
    setTipo('Geral');
    setShowForm(true);
  };

  const abrirEdicao = (ponto: PontoAcessoResponse) => {
    setEditando(ponto);
    setNome(ponto.nome);
    setTipo(ponto.tipo);
    setShowForm(true);
  };

  const salvar = async () => {
    if (!nome.trim()) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      if (editando) {
        const atualizado = await api.put<PontoAcessoResponse>(`/api/pontos-acesso/${editando.id}`, { nome: nome.trim(), tipo });
        setPontos((prev) => prev.map((p) => (p.id === atualizado.id ? atualizado : p)));
      } else {
        const criado = await api.post<PontoAcessoResponse>(`/api/properties/${propriedadeId}/pontos-acesso`, {
          nome: nome.trim(),
          tipo,
        });
        setPontos((prev) => [...prev, criado]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar o ponto de acesso.');
    } finally {
      setSalvando(false);
    }
  };

  const confirmarExclusao = (ponto: PontoAcessoResponse) => {
    Alert.alert(
      'Excluir ponto de acesso?',
      `"${ponto.nome}" e as permissões vinculadas a ele deixarão de aparecer no app.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Excluir',
          style: 'destructive',
          onPress: async () => {
            try {
              await api.delete(`/api/pontos-acesso/${ponto.id}`);
              setPontos((prev) => prev.filter((p) => p.id !== ponto.id));
            } catch (err) {
              setError(err instanceof ApiError ? err.message : 'Não foi possível excluir o ponto de acesso.');
            }
          },
        },
      ],
    );
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Pontos de acesso</Text>
          <Text style={styles.subtitle}>{nomePropriedade}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {loading ? (
        <View style={{ gap: spacing.sm }}>
          <Skeleton height={64} radius={radius.lg} />
          <Skeleton height={64} radius={radius.lg} />
        </View>
      ) : (
        <FlatList
          data={pontos}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <View style={styles.cardMain}>
                <View style={styles.cardIcon}>
                  {item.tipo === 'Veicular' ? <Car size={18} color={colors.safe} /> : <DoorOpen size={18} color={colors.safe} />}
                </View>
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>{item.nome}</Text>
                  {item.tipo === 'Veicular' ? <Text style={styles.cardSubtitle}>Veicular</Text> : null}
                </View>
              </View>
              <View style={styles.cardActions}>
                <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar ponto de acesso">
                  <Pencil size={16} color={colors.sub} />
                </Pressable>
                <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir ponto de acesso">
                  <Trash2 size={16} color={colors.danger} />
                </Pressable>
              </View>
            </View>
          )}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={DoorOpen}
                titulo="Nenhum ponto de acesso ainda"
                descricao="Cadastre os locais que terão controle de acesso, como o portão principal ou a garagem."
                cta={{ label: 'Adicionar ponto de acesso', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Nome" value={nome} onChangeText={setNome} placeholder="Ex.: Portão Principal, Piscina" />
          <View style={styles.tipoSelectorBlock}>
            <Text style={styles.label}>Tipo</Text>
            <View style={styles.chipsRow}>
              {(['Geral', 'Veicular'] as const).map((opcao) => {
                const ativo = opcao === tipo;
                return (
                  <Pressable key={opcao} onPress={() => setTipo(opcao)} style={[styles.chip, ativo && styles.chipAtivo]}>
                    <Text style={[styles.chipLabel, ativo && styles.chipLabelAtivo]}>{opcao === 'Veicular' ? 'Veicular' : 'Geral'}</Text>
                  </Pressable>
                );
              })}
            </View>
          </View>
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar ponto de acesso'}
            onPress={salvar}
            loading={salvando}
            disabled={!nome.trim()}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : (
        <PrimaryButton label="Adicionar ponto de acesso" variant="secondary" onPress={abrirNovo} />
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
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
  },
  cardMain: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: spacing.md, padding: spacing.md },
  cardIcon: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.safeDim,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cardTextWrap: { flex: 1 },
  cardTitle: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  cardSubtitle: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  cardActions: { flexDirection: 'row', gap: spacing.xs, paddingRight: spacing.md },
  actionBtn: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  form: { gap: spacing.sm },
  tipoSelectorBlock: { marginBottom: spacing.lg },
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
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
