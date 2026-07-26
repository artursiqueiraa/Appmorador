import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, ParkingSquare, Pencil, Trash2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { StatusVaga, TipoVaga, VagaResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { TipoVagaSelector, rotuloTipoVaga } from '../../components/TipoVagaSelector';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type VagasRouteProp = RouteProp<RootStackParamList, 'Vagas'>;

const FORM_INICIAL = { numero: '', bloco: '', andar: '', observacoes: '' };

const STATUS_COR: Record<StatusVaga, string> = {
  Livre: colors.safe,
  Ocupada: colors.accent,
  Bloqueada: colors.danger,
  Reservada: colors.mute,
};

/**
 * Sprint 9 — Veículos e Garagens. Mesmo padrão de tela de `PontosAcessoScreen` (lista
 * + formulário inline). Status Livre/Ocupada é computado pelo backend a partir do
 * vínculo ativo — só Bloqueada/Reservada são ações manuais explícitas (ver ADR 0012).
 */
export function VagasScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<VagasRouteProp>();
  const { propriedadeId, nomePropriedade } = params;

  const [vagas, setVagas] = useState<VagaResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<VagaResponse | null>(null);
  const [form, setForm] = useState(FORM_INICIAL);
  const [coberta, setCoberta] = useState(false);
  const [tipo, setTipo] = useState<TipoVaga | null>(null);
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const lista = await api.get<VagaResponse[]>(`/api/properties/${propriedadeId}/vagas`);
      setVagas(lista);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar as vagas.');
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
    setCoberta(false);
    setTipo(null);
    setShowForm(true);
  };

  const abrirEdicao = (vaga: VagaResponse) => {
    setEditando(vaga);
    setForm({ numero: vaga.numero, bloco: vaga.bloco ?? '', andar: vaga.andar ?? '', observacoes: vaga.observacoes ?? '' });
    setCoberta(vaga.coberta);
    setTipo(vaga.tipo);
    setShowForm(true);
  };

  const salvar = async () => {
    if (!form.numero.trim() || !tipo) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      const payload = {
        numero: form.numero.trim(),
        bloco: form.bloco.trim() || undefined,
        andar: form.andar.trim() || undefined,
        coberta,
        tipo,
        observacoes: form.observacoes.trim() || undefined,
      };
      if (editando) {
        const atualizada = await api.put<VagaResponse>(`/api/vagas/${editando.id}`, payload);
        setVagas((prev) => prev.map((v) => (v.id === atualizada.id ? atualizada : v)));
      } else {
        const criada = await api.post<VagaResponse>(`/api/properties/${propriedadeId}/vagas`, payload);
        setVagas((prev) => [...prev, criada]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar a vaga.');
    } finally {
      setSalvando(false);
    }
  };

  const alterarStatus = (vaga: VagaResponse) => {
    if (vaga.status === 'Ocupada') {
      Alert.alert('Vaga ocupada', 'Esta vaga está ocupada por um veículo agora — desvincule o veículo antes de alterar o status.');
      return;
    }

    const opcoes: StatusVaga[] = (['Livre', 'Bloqueada', 'Reservada'] as StatusVaga[]).filter((s) => s !== vaga.status);
    Alert.alert('Alterar status da vaga', `Vaga ${vaga.numero} está ${vaga.status.toLowerCase()}. Selecione o novo status:`, [
      ...opcoes.map((status) => ({
        text: status,
        style: status === 'Bloqueada' ? ('destructive' as const) : ('default' as const),
        onPress: async () => {
          try {
            const atualizada = await api.put<VagaResponse>(`/api/vagas/${vaga.id}/status`, { status });
            setVagas((prev) => prev.map((v) => (v.id === atualizada.id ? atualizada : v)));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível atualizar o status.');
          }
        },
      })),
      { text: 'Cancelar', style: 'cancel' as const },
    ]);
  };

  const confirmarExclusao = (vaga: VagaResponse) => {
    Alert.alert('Excluir vaga?', `"${vaga.numero}" e o vínculo de veículo (se houver) deixarão de aparecer no app.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Excluir',
        style: 'destructive',
        onPress: async () => {
          try {
            await api.delete(`/api/vagas/${vaga.id}`);
            setVagas((prev) => prev.filter((v) => v.id !== vaga.id));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível excluir a vaga.');
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
          <Text style={styles.title}>Vagas</Text>
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
          data={vagas}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <View style={styles.cardMain}>
                <View style={styles.cardIcon}>
                  <ParkingSquare size={18} color={STATUS_COR[item.status]} />
                </View>
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>
                    {item.numero}
                    {item.bloco ? ` • Bloco ${item.bloco}` : ''}
                  </Text>
                  <Text style={styles.cardSubtitle}>
                    {rotuloTipoVaga(item.tipo)}
                    {item.coberta ? ' • Coberta' : ''}
                  </Text>
                </View>
                <Pressable onPress={() => alterarStatus(item)} accessibilityLabel="Alterar status">
                  <Text style={[styles.statusLabel, { color: STATUS_COR[item.status] }]}>{item.status}</Text>
                </Pressable>
              </View>
              <View style={styles.cardActions}>
                <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar vaga">
                  <Pencil size={16} color={colors.sub} />
                </Pressable>
                <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir vaga">
                  <Trash2 size={16} color={colors.danger} />
                </Pressable>
              </View>
            </View>
          )}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={ParkingSquare}
                titulo="Nenhuma vaga ainda"
                descricao="Cadastre as vagas da propriedade para depois vincular veículos a elas."
                cta={{ label: 'Adicionar vaga', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Número" value={form.numero} onChangeText={(v) => setForm((f) => ({ ...f, numero: v }))} placeholder="Ex.: 12" />
          <TextField label="Bloco (opcional)" value={form.bloco} onChangeText={(v) => setForm((f) => ({ ...f, bloco: v }))} placeholder="Ex.: A" />
          <TextField label="Andar (opcional)" value={form.andar} onChangeText={(v) => setForm((f) => ({ ...f, andar: v }))} placeholder="Ex.: Térreo" />
          <TipoVagaSelector label="Tipo" value={tipo} onChange={setTipo} />
          <Pressable onPress={() => setCoberta((c) => !c)} style={styles.cobertaRow}>
            <View style={[styles.checkbox, coberta && styles.checkboxAtivo]} />
            <Text style={styles.cobertaLabel}>Vaga coberta</Text>
          </Pressable>
          <TextField
            label="Observações (opcional)"
            value={form.observacoes}
            onChangeText={(v) => setForm((f) => ({ ...f, observacoes: v }))}
            placeholder="Ex.: próxima à entrada de serviço"
          />
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar vaga'}
            onPress={salvar}
            loading={salvando}
            disabled={!form.numero.trim() || !tipo}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : (
        <PrimaryButton label="Adicionar vaga" variant="secondary" onPress={abrirNovo} />
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
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cardTextWrap: { flex: 1 },
  cardTitle: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  cardSubtitle: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  statusLabel: { fontSize: fontSize.label, fontWeight: fontWeight.medium },
  cardActions: { flexDirection: 'row', gap: spacing.xs },
  actionBtn: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  form: { gap: spacing.sm },
  cobertaRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, marginBottom: spacing.lg },
  checkbox: { width: 20, height: 20, borderRadius: radius.sm, borderWidth: 1, borderColor: colors.line, backgroundColor: colors.surface },
  checkboxAtivo: { backgroundColor: colors.safe, borderColor: colors.safeLine },
  cobertaLabel: { color: colors.text, fontSize: fontSize.secondary },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
