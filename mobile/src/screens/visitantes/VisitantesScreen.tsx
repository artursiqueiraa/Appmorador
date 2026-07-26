import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, ChevronRight, Trash2, UserSquare2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { VisitanteResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type VisitantesRouteProp = RouteProp<RootStackParamList, 'Visitantes'>;

const FORM_INICIAL = { nome: '', documento: '', telefone: '', observacoes: '' };

/**
 * Sprint 8 — Visitantes e Autorizações. Mesmo padrão de tela de `PontosAcessoScreen`
 * (lista + formulário inline). Visitante pertence direto à Propriedade (ADR 0011) —
 * reaproveitável em autorizações de unidades diferentes.
 */
export function VisitantesScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<VisitantesRouteProp>();
  const { propriedadeId, nomePropriedade } = params;

  const [visitantes, setVisitantes] = useState<VisitanteResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<VisitanteResponse | null>(null);
  const [form, setForm] = useState(FORM_INICIAL);
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const lista = await api.get<VisitanteResponse[]>(`/api/properties/${propriedadeId}/visitantes`);
      setVisitantes(lista);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar os visitantes.');
    } finally {
      setLoading(false);
    }
  }, [propriedadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const abrirNovo = () => {
    setEditando(null);
    setForm(FORM_INICIAL);
    setShowForm(true);
  };

  const abrirEdicao = (visitante: VisitanteResponse) => {
    setEditando(visitante);
    setForm({
      nome: visitante.nome,
      documento: visitante.documento ?? '',
      telefone: visitante.telefone ?? '',
      observacoes: visitante.observacoes ?? '',
    });
    setShowForm(true);
  };

  const salvar = async () => {
    if (!form.nome.trim()) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      if (editando) {
        const atualizado = await api.put<VisitanteResponse>(`/api/visitantes/${editando.id}`, {
          nome: form.nome.trim(),
          documento: form.documento.trim() || undefined,
          telefone: form.telefone.trim() || undefined,
          observacoes: form.observacoes.trim() || undefined,
        });
        setVisitantes((prev) => prev.map((v) => (v.id === atualizado.id ? atualizado : v)));
      } else {
        const criado = await api.post<VisitanteResponse>(`/api/properties/${propriedadeId}/visitantes`, {
          nome: form.nome.trim(),
          documento: form.documento.trim() || undefined,
          telefone: form.telefone.trim() || undefined,
          observacoes: form.observacoes.trim() || undefined,
        });
        setVisitantes((prev) => [...prev, criado]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar o visitante.');
    } finally {
      setSalvando(false);
    }
  };

  const confirmarExclusao = (visitante: VisitanteResponse) => {
    Alert.alert(
      'Excluir visitante?',
      `"${visitante.nome}" e as autorizações vinculadas a ele deixarão de aparecer no app.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Excluir',
          style: 'destructive',
          onPress: async () => {
            try {
              await api.delete(`/api/visitantes/${visitante.id}`);
              setVisitantes((prev) => prev.filter((v) => v.id !== visitante.id));
            } catch (err) {
              setError(err instanceof ApiError ? err.message : 'Não foi possível excluir o visitante.');
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
          <Text style={styles.title}>Visitantes</Text>
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
          data={visitantes}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <Pressable
                style={styles.cardMain}
                onPress={() => navigation.navigate('Autorizacoes', { visitanteId: item.id, nomeVisitante: item.nome, propriedadeId })}
              >
                <View style={styles.cardIcon}>
                  <UserSquare2 size={18} color={colors.safe} />
                </View>
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>{item.nome}</Text>
                  {item.telefone ? <Text style={styles.cardSubtitle}>{item.telefone}</Text> : null}
                </View>
                <ChevronRight size={18} color={colors.mute} />
              </Pressable>
              <View style={styles.cardActions}>
                <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar visitante">
                  <Text style={styles.editLabel}>Editar</Text>
                </Pressable>
                <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir visitante">
                  <Trash2 size={16} color={colors.danger} />
                </Pressable>
              </View>
            </View>
          )}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={UserSquare2}
                titulo="Nenhum visitante ainda"
                descricao="Cadastre quem pode visitar esta propriedade para depois criar autorizações de acesso."
                cta={{ label: 'Adicionar visitante', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Nome" value={form.nome} onChangeText={(v) => setForm((f) => ({ ...f, nome: v }))} placeholder="Nome completo" />
          <TextField
            label="Documento (opcional)"
            value={form.documento}
            onChangeText={(v) => setForm((f) => ({ ...f, documento: v }))}
            placeholder="CPF ou RG"
          />
          <TextField
            label="Telefone (opcional)"
            value={form.telefone}
            onChangeText={(v) => setForm((f) => ({ ...f, telefone: v }))}
            placeholder="(00) 00000-0000"
            keyboardType="phone-pad"
          />
          <TextField
            label="Observações (opcional)"
            value={form.observacoes}
            onChangeText={(v) => setForm((f) => ({ ...f, observacoes: v }))}
            placeholder="Ex.: entregador frequente, prestador cadastrado"
          />
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar visitante'}
            onPress={salvar}
            loading={salvando}
            disabled={!form.nome.trim()}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : (
        <PrimaryButton label="Adicionar visitante" variant="secondary" onPress={abrirNovo} />
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
  cardMain: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: spacing.md },
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
  cardActions: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
  actionBtn: { minHeight: 32, alignItems: 'center', justifyContent: 'center' },
  editLabel: { color: colors.sub, fontSize: fontSize.label, fontWeight: fontWeight.medium },
  form: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
