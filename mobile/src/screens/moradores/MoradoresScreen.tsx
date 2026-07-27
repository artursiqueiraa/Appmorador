import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Car, ChevronLeft, ChevronRight, Pencil, Trash2, User, UserCheck, UserX } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { MoradorResponse, StatusMorador } from '../../api/types';
import { usePermissao } from '../../auth/usePermissao';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type MoradoresRouteProp = RouteProp<RootStackParamList, 'Moradores'>;

const FORM_INICIAL = { nome: '', telefone: '', email: '', documento: '', observacoes: '' };

/**
 * Sprint 6 — Domínio do Produto. Mesmo padrão de `UnidadesScreen`/
 * `SelecionarPropriedadeScreen` (lista + formulário inline). Foto/biometria não são
 * implementadas aqui — o campo existe no contrato (`fotoPath`), mas nunca é
 * preenchido nesta Sprint (ver docs/DIVIDA_TECNICA.md).
 */
export function MoradoresScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<MoradoresRouteProp>();
  const { unidadeId, identificacaoUnidade, propriedadeId } = params;
  const { temPermissao } = usePermissao();
  const podeCadastrarMorador = temPermissao('CadastrarMorador');

  const [moradores, setMoradores] = useState<MoradorResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<MoradorResponse | null>(null);
  const [form, setForm] = useState(FORM_INICIAL);
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const lista = await api.get<MoradorResponse[]>(`/api/unidades/${unidadeId}/moradores`);
      setMoradores(lista);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar os moradores.');
    } finally {
      setLoading(false);
    }
  }, [unidadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const abrirNovo = () => {
    setEditando(null);
    setForm(FORM_INICIAL);
    setShowForm(true);
  };

  const abrirEdicao = (morador: MoradorResponse) => {
    setEditando(morador);
    setForm({
      nome: morador.nome,
      telefone: morador.telefone ?? '',
      email: morador.email ?? '',
      documento: morador.documento ?? '',
      observacoes: morador.observacoes ?? '',
    });
    setShowForm(true);
  };

  const alternarStatus = async (morador: MoradorResponse) => {
    const novoStatus: StatusMorador = morador.status === 'Ativo' ? 'Inativo' : 'Ativo';
    try {
      const atualizado = await api.put<MoradorResponse>(`/api/moradores/${morador.id}`, {
        nome: morador.nome,
        telefone: morador.telefone,
        email: morador.email,
        documento: morador.documento,
        observacoes: morador.observacoes,
        status: novoStatus,
      });
      setMoradores((prev) => prev.map((m) => (m.id === atualizado.id ? atualizado : m)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível atualizar o status.');
    }
  };

  const salvar = async () => {
    if (!form.nome.trim()) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      if (editando) {
        const atualizado = await api.put<MoradorResponse>(`/api/moradores/${editando.id}`, {
          nome: form.nome.trim(),
          telefone: form.telefone.trim() || undefined,
          email: form.email.trim() || undefined,
          documento: form.documento.trim() || undefined,
          observacoes: form.observacoes.trim() || undefined,
          status: editando.status,
        });
        setMoradores((prev) => prev.map((m) => (m.id === atualizado.id ? atualizado : m)));
      } else {
        const criado = await api.post<MoradorResponse>(`/api/unidades/${unidadeId}/moradores`, {
          nome: form.nome.trim(),
          telefone: form.telefone.trim() || undefined,
          email: form.email.trim() || undefined,
          documento: form.documento.trim() || undefined,
          observacoes: form.observacoes.trim() || undefined,
        });
        setMoradores((prev) => [...prev, criado]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar o morador.');
    } finally {
      setSalvando(false);
    }
  };

  const confirmarExclusao = (morador: MoradorResponse) => {
    Alert.alert('Excluir morador?', `"${morador.nome}" deixará de aparecer no app.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Excluir',
        style: 'destructive',
        onPress: async () => {
          try {
            await api.delete(`/api/moradores/${morador.id}`);
            setMoradores((prev) => prev.filter((m) => m.id !== morador.id));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível excluir o morador.');
          }
        },
      },
    ]);
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Moradores</Text>
          <Text style={styles.subtitle}>{identificacaoUnidade}</Text>
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
          data={moradores}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => {
            const ativo = item.status === 'Ativo';
            return (
              <View style={styles.card}>
                <Pressable
                  style={styles.cardMain}
                  onPress={() => navigation.navigate('Credenciais', { moradorId: item.id, nomeMorador: item.nome, propriedadeId })}
                >
                  <View style={styles.cardIcon}>
                    <User size={18} color={ativo ? colors.safe : colors.mute} />
                  </View>
                  <View style={styles.cardTextWrap}>
                    <Text style={styles.cardTitle}>{item.nome}</Text>
                    {item.telefone ? <Text style={styles.cardSubtitle}>{item.telefone}</Text> : null}
                  </View>
                  <ChevronRight size={18} color={colors.mute} />
                </Pressable>
                <Pressable onPress={() => alternarStatus(item)} style={styles.statusBadge} accessibilityLabel="Alternar status">
                  {ativo ? <UserCheck size={13} color={colors.safe} /> : <UserX size={13} color={colors.mute} />}
                  <Text style={[styles.statusLabel, { color: ativo ? colors.safe : colors.mute }]}>{item.status}</Text>
                </Pressable>
                <View style={styles.cardActions}>
                  <Pressable
                    onPress={() => navigation.navigate('Veiculos', { moradorId: item.id, nomeMorador: item.nome, propriedadeId })}
                    style={styles.actionBtn}
                    accessibilityLabel="Ver veículos"
                  >
                    <Car size={16} color={colors.sub} />
                  </Pressable>
                  <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar morador">
                    <Pencil size={16} color={colors.sub} />
                  </Pressable>
                  <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir morador">
                    <Trash2 size={16} color={colors.danger} />
                  </Pressable>
                </View>
              </View>
            );
          }}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={User}
                titulo="Nenhum morador ainda"
                descricao="Cadastre quem mora nesta unidade para manter o controle da propriedade em dia."
                cta={podeCadastrarMorador ? { label: 'Adicionar morador', onPress: abrirNovo } : undefined}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Nome" value={form.nome} onChangeText={(v) => setForm((f) => ({ ...f, nome: v }))} placeholder="Nome completo" />
          <TextField
            label="Telefone (opcional)"
            value={form.telefone}
            onChangeText={(v) => setForm((f) => ({ ...f, telefone: v }))}
            placeholder="(00) 00000-0000"
            keyboardType="phone-pad"
          />
          <TextField
            label="E-mail (opcional)"
            value={form.email}
            onChangeText={(v) => setForm((f) => ({ ...f, email: v }))}
            placeholder="email@exemplo.com"
            keyboardType="email-address"
            autoCapitalize="none"
          />
          <TextField
            label="Documento (opcional)"
            value={form.documento}
            onChangeText={(v) => setForm((f) => ({ ...f, documento: v }))}
            placeholder="CPF ou RG"
          />
          <TextField
            label="Observações (opcional)"
            value={form.observacoes}
            onChangeText={(v) => setForm((f) => ({ ...f, observacoes: v }))}
            placeholder="Ex.: cônjuge, filho, inquilino"
          />
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar morador'}
            onPress={salvar}
            loading={salvando}
            disabled={!form.nome.trim()}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : podeCadastrarMorador ? (
        <PrimaryButton label="Adicionar morador" variant="secondary" onPress={abrirNovo} />
      ) : null}
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
  cardMain: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
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
  statusBadge: { flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: spacing.xs, paddingVertical: 4 },
  statusLabel: { fontSize: fontSize.label, fontWeight: fontWeight.medium },
  cardActions: { flexDirection: 'row', gap: spacing.xs },
  actionBtn: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  form: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
