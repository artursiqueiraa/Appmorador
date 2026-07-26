import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, ChevronRight, Home, Pencil, Trash2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { UnidadeResponse, TipoUnidade } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { TipoUnidadeSelector, rotuloTipoUnidade } from '../../components/TipoUnidadeSelector';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type UnidadesRouteProp = RouteProp<RootStackParamList, 'Unidades'>;

/**
 * Sprint 6 — Domínio do Produto. Orquestrador: busca/cria/edita/exclui Unidades de
 * uma Propriedade. Mesmo padrão de tela de `SelecionarPropriedadeScreen` (lista +
 * formulário inline), reaproveitado em vez de inventar um padrão novo.
 */
export function UnidadesScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<UnidadesRouteProp>();
  const { propriedadeId, nomePropriedade } = params;

  const [unidades, setUnidades] = useState<UnidadeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<UnidadeResponse | null>(null);
  const [tipo, setTipo] = useState<TipoUnidade | null>(null);
  const [identificacao, setIdentificacao] = useState('');
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const lista = await api.get<UnidadeResponse[]>(`/api/properties/${propriedadeId}/unidades`);
      setUnidades(lista);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar as unidades.');
    } finally {
      setLoading(false);
    }
  }, [propriedadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const abrirNovo = () => {
    setEditando(null);
    setTipo(null);
    setIdentificacao('');
    setShowForm(true);
  };

  const abrirEdicao = (unidade: UnidadeResponse) => {
    setEditando(unidade);
    setTipo(unidade.tipo);
    setIdentificacao(unidade.identificacao);
    setShowForm(true);
  };

  const salvar = async () => {
    if (!tipo || !identificacao.trim()) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      if (editando) {
        const atualizada = await api.put<UnidadeResponse>(`/api/unidades/${editando.id}`, {
          tipo,
          identificacao: identificacao.trim(),
        });
        setUnidades((prev) => prev.map((u) => (u.id === atualizada.id ? atualizada : u)));
      } else {
        const criada = await api.post<UnidadeResponse>(`/api/properties/${propriedadeId}/unidades`, {
          tipo,
          identificacao: identificacao.trim(),
        });
        setUnidades((prev) => [...prev, criada]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar a unidade.');
    } finally {
      setSalvando(false);
    }
  };

  const confirmarExclusao = (unidade: UnidadeResponse) => {
    Alert.alert(
      'Excluir unidade?',
      `"${unidade.identificacao}" e os moradores cadastrados nela deixarão de aparecer no app.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Excluir',
          style: 'destructive',
          onPress: async () => {
            try {
              await api.delete(`/api/unidades/${unidade.id}`);
              setUnidades((prev) => prev.filter((u) => u.id !== unidade.id));
            } catch (err) {
              setError(err instanceof ApiError ? err.message : 'Não foi possível excluir a unidade.');
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
          <Text style={styles.title}>Unidades</Text>
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
          data={unidades}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <Pressable
                style={styles.cardMain}
                onPress={() =>
                  navigation.navigate('Moradores', {
                    unidadeId: item.id,
                    identificacaoUnidade: item.identificacao,
                    propriedadeId,
                  })
                }
              >
                <View style={styles.cardIcon}>
                  <Home size={18} color={colors.safe} />
                </View>
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>{item.identificacao}</Text>
                  <Text style={styles.cardSubtitle}>{rotuloTipoUnidade(item.tipo)}</Text>
                </View>
                <ChevronRight size={18} color={colors.mute} />
              </Pressable>
              <View style={styles.cardActions}>
                <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar unidade">
                  <Pencil size={16} color={colors.sub} />
                </Pressable>
                <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir unidade">
                  <Trash2 size={16} color={colors.danger} />
                </Pressable>
              </View>
            </View>
          )}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={Home}
                titulo="Nenhuma unidade ainda"
                descricao="Cadastre a primeira unidade desta propriedade para começar a organizar os moradores."
                cta={{ label: 'Adicionar unidade', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          <TipoUnidadeSelector label="Tipo de unidade" value={tipo} onChange={setTipo} />
          <TextField
            label="Identificação"
            value={identificacao}
            onChangeText={setIdentificacao}
            placeholder="Ex.: 302, Bloco B - Casa 12"
          />
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar unidade'}
            onPress={salvar}
            loading={salvando}
            disabled={!tipo || !identificacao.trim()}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : (
        <PrimaryButton label="Adicionar unidade" variant="secondary" onPress={abrirNovo} />
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
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
