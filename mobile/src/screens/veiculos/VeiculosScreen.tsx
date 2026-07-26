import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Car, ChevronDown, ChevronLeft, ChevronUp, Pencil, Trash2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { StatusVeiculo, TipoVeiculo, VagaResponse, VeiculoResponse, VinculoVeiculoVagaResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { TipoVeiculoSelector, rotuloTipoVeiculo } from '../../components/TipoVeiculoSelector';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type VeiculosRouteProp = RouteProp<RootStackParamList, 'Veiculos'>;

const FORM_INICIAL = { placa: '', marca: '', modelo: '', cor: '', ano: '', observacoes: '' };
const TODOS_STATUS: StatusVeiculo[] = ['Ativo', 'Suspenso', 'Inativo'];

const STATUS_COR: Record<StatusVeiculo, string> = {
  Ativo: colors.safe,
  Suspenso: colors.accent,
  Inativo: colors.mute,
};

/**
 * Sprint 9 — Veículos e Garagens. Mesmo padrão de tela de `MoradoresScreen` (lista +
 * formulário inline). O painel de vínculo com uma Vaga é carregado sob demanda (só
 * quando o card é expandido) para não fazer N chamadas extras ao simplesmente listar
 * os veículos.
 */
export function VeiculosScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<VeiculosRouteProp>();
  const { moradorId, nomeMorador, propriedadeId } = params;

  const [veiculos, setVeiculos] = useState<VeiculoResponse[]>([]);
  const [vagas, setVagas] = useState<VagaResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<VeiculoResponse | null>(null);
  const [form, setForm] = useState(FORM_INICIAL);
  const [tipo, setTipo] = useState<TipoVeiculo | null>(null);
  const [salvando, setSalvando] = useState(false);

  const [expandidoId, setExpandidoId] = useState<string | null>(null);
  const [vinculoAtivo, setVinculoAtivo] = useState<VinculoVeiculoVagaResponse | null>(null);
  const [carregandoVinculo, setCarregandoVinculo] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [listaVeiculos, listaVagas] = await Promise.all([
        api.get<VeiculoResponse[]>(`/api/moradores/${moradorId}/veiculos`),
        api.get<VagaResponse[]>(`/api/properties/${propriedadeId}/vagas`),
      ]);
      setVeiculos(listaVeiculos);
      setVagas(listaVagas);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar os veículos.');
    } finally {
      setLoading(false);
    }
  }, [moradorId, propriedadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const alternarExpansao = async (veiculo: VeiculoResponse) => {
    if (expandidoId === veiculo.id) {
      setExpandidoId(null);
      return;
    }

    setExpandidoId(veiculo.id);
    setCarregandoVinculo(true);
    setVinculoAtivo(null);
    try {
      const historico = await api.get<VinculoVeiculoVagaResponse[]>(`/api/veiculos/${veiculo.id}/vinculos`);
      setVinculoAtivo(historico.find((v) => !v.dataFimUtc) ?? null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar o vínculo do veículo.');
    } finally {
      setCarregandoVinculo(false);
    }
  };

  const vincular = async (veiculoId: string, vagaId: string) => {
    try {
      const novoVinculo = await api.put<VinculoVeiculoVagaResponse>(`/api/veiculos/${veiculoId}/vinculo`, { vagaId });
      setVinculoAtivo(novoVinculo);
      await carregar();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível vincular o veículo à vaga.');
    }
  };

  const desvincular = async (veiculoId: string) => {
    try {
      await api.delete(`/api/veiculos/${veiculoId}/vinculo`);
      setVinculoAtivo(null);
      await carregar();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível desvincular o veículo.');
    }
  };

  const abrirNovo = () => {
    setEditando(null);
    setForm(FORM_INICIAL);
    setTipo(null);
    setShowForm(true);
  };

  const abrirEdicao = (veiculo: VeiculoResponse) => {
    setEditando(veiculo);
    setForm({
      placa: veiculo.placa,
      marca: veiculo.marca ?? '',
      modelo: veiculo.modelo ?? '',
      cor: veiculo.cor ?? '',
      ano: veiculo.ano?.toString() ?? '',
      observacoes: veiculo.observacoes ?? '',
    });
    setTipo(veiculo.tipo);
    setShowForm(true);
  };

  const salvar = async () => {
    if (!form.placa.trim() || !tipo) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      const anoNumero = form.ano.trim() ? Number(form.ano.trim()) : undefined;
      if (editando) {
        const atualizado = await api.put<VeiculoResponse>(`/api/veiculos/${editando.id}`, {
          placa: form.placa.trim(),
          marca: form.marca.trim() || undefined,
          modelo: form.modelo.trim() || undefined,
          cor: form.cor.trim() || undefined,
          ano: anoNumero,
          observacoes: form.observacoes.trim() || undefined,
          tipo,
          status: editando.status,
        });
        setVeiculos((prev) => prev.map((v) => (v.id === atualizado.id ? atualizado : v)));
      } else {
        const criado = await api.post<VeiculoResponse>(`/api/moradores/${moradorId}/veiculos`, {
          placa: form.placa.trim(),
          marca: form.marca.trim() || undefined,
          modelo: form.modelo.trim() || undefined,
          cor: form.cor.trim() || undefined,
          ano: anoNumero,
          observacoes: form.observacoes.trim() || undefined,
          tipo,
        });
        setVeiculos((prev) => [...prev, criado]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar o veículo.');
    } finally {
      setSalvando(false);
    }
  };

  const alterarStatus = (veiculo: VeiculoResponse) => {
    const opcoes = TODOS_STATUS.filter((s) => s !== veiculo.status);
    Alert.alert('Alterar status do veículo', `${veiculo.placa} está ${veiculo.status.toLowerCase()}. Selecione o novo status:`, [
      ...opcoes.map((status) => ({
        text: status,
        onPress: async () => {
          try {
            const atualizado = await api.put<VeiculoResponse>(`/api/veiculos/${veiculo.id}`, {
              placa: veiculo.placa,
              marca: veiculo.marca,
              modelo: veiculo.modelo,
              cor: veiculo.cor,
              ano: veiculo.ano,
              observacoes: veiculo.observacoes,
              tipo: veiculo.tipo,
              status,
            });
            setVeiculos((prev) => prev.map((v) => (v.id === atualizado.id ? atualizado : v)));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível atualizar o status.');
          }
        },
      })),
      { text: 'Cancelar', style: 'cancel' as const },
    ]);
  };

  const confirmarExclusao = (veiculo: VeiculoResponse) => {
    Alert.alert('Excluir veículo?', `"${veiculo.placa}" deixará de aparecer no app.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Excluir',
        style: 'destructive',
        onPress: async () => {
          try {
            await api.delete(`/api/veiculos/${veiculo.id}`);
            setVeiculos((prev) => prev.filter((v) => v.id !== veiculo.id));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível excluir o veículo.');
          }
        },
      },
    ]);
  };

  const vagasLivres = vagas.filter((v) => v.status === 'Livre');

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Veículos</Text>
          <Text style={styles.subtitle}>{nomeMorador}</Text>
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
          data={veiculos}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => {
            const expandido = expandidoId === item.id;
            return (
              <View style={styles.card}>
                <View style={styles.cardHeader}>
                  <View style={styles.cardIcon}>
                    <Car size={18} color={STATUS_COR[item.status]} />
                  </View>
                  <View style={styles.cardTextWrap}>
                    <Text style={styles.cardTitle}>{item.placa}</Text>
                    <Text style={styles.cardSubtitle}>
                      {[item.marca, item.modelo].filter(Boolean).join(' ') || rotuloTipoVeiculo(item.tipo)}
                    </Text>
                  </View>
                  <Pressable onPress={() => alterarStatus(item)} accessibilityLabel="Alterar status">
                    <Text style={[styles.statusLabel, { color: STATUS_COR[item.status] }]}>{item.status}</Text>
                  </Pressable>
                </View>
                <View style={styles.cardActions}>
                  <Pressable onPress={() => alternarExpansao(item)} style={styles.actionBtnRow}>
                    <Text style={styles.actionLabel}>Vaga</Text>
                    {expandido ? <ChevronUp size={14} color={colors.sub} /> : <ChevronDown size={14} color={colors.sub} />}
                  </Pressable>
                  <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar veículo">
                    <Pencil size={16} color={colors.sub} />
                  </Pressable>
                  <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir veículo">
                    <Trash2 size={16} color={colors.danger} />
                  </Pressable>
                </View>

                {expandido ? (
                  <View style={styles.vinculoPanel}>
                    {carregandoVinculo ? (
                      <Text style={styles.vinculoTexto}>Carregando...</Text>
                    ) : vinculoAtivo ? (
                      <>
                        <Text style={styles.vinculoTexto}>Vinculado à vaga {vinculoAtivo.vagaNumero}</Text>
                        <PrimaryButton label="Desvincular" variant="secondary" onPress={() => desvincular(item.id)} />
                      </>
                    ) : vagasLivres.length === 0 ? (
                      <Text style={styles.vinculoTexto}>Não vinculado. Nenhuma vaga livre no momento.</Text>
                    ) : (
                      <>
                        <Text style={styles.vinculoTexto}>Não vinculado. Escolha uma vaga livre:</Text>
                        <View style={styles.chipsRow}>
                          {vagasLivres.map((vaga) => (
                            <Pressable key={vaga.id} onPress={() => vincular(item.id, vaga.id)} style={styles.chip}>
                              <Text style={styles.chipLabel}>{vaga.numero}</Text>
                            </Pressable>
                          ))}
                        </View>
                      </>
                    )}
                  </View>
                ) : null}
              </View>
            );
          }}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={Car}
                titulo="Nenhum veículo ainda"
                descricao="Cadastre o primeiro veículo deste morador para depois vinculá-lo a uma vaga."
                cta={{ label: 'Adicionar veículo', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Placa" value={form.placa} onChangeText={(v) => setForm((f) => ({ ...f, placa: v }))} placeholder="ABC1234" autoCapitalize="characters" />
          <TipoVeiculoSelector label="Tipo" value={tipo} onChange={setTipo} />
          <TextField label="Marca (opcional)" value={form.marca} onChangeText={(v) => setForm((f) => ({ ...f, marca: v }))} placeholder="Ex.: Toyota" />
          <TextField label="Modelo (opcional)" value={form.modelo} onChangeText={(v) => setForm((f) => ({ ...f, modelo: v }))} placeholder="Ex.: Corolla" />
          <TextField label="Cor (opcional)" value={form.cor} onChangeText={(v) => setForm((f) => ({ ...f, cor: v }))} placeholder="Ex.: Prata" />
          <TextField label="Ano (opcional)" value={form.ano} onChangeText={(v) => setForm((f) => ({ ...f, ano: v }))} placeholder="2022" keyboardType="number-pad" />
          <TextField
            label="Observações (opcional)"
            value={form.observacoes}
            onChangeText={(v) => setForm((f) => ({ ...f, observacoes: v }))}
            placeholder="Ex.: veículo de uso ocasional"
          />
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar veículo'}
            onPress={salvar}
            loading={salvando}
            disabled={!form.placa.trim() || !tipo}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : (
        <PrimaryButton label="Adicionar veículo" variant="secondary" onPress={abrirNovo} />
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
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
    gap: spacing.sm,
  },
  cardHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
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
  cardActions: { flexDirection: 'row', gap: spacing.md, alignItems: 'center' },
  actionBtn: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  actionBtnRow: { flexDirection: 'row', alignItems: 'center', gap: 4, minHeight: 28 },
  actionLabel: { color: colors.sub, fontSize: fontSize.label, fontWeight: fontWeight.medium },
  vinculoPanel: { borderTopWidth: 1, borderTopColor: colors.line, paddingTop: spacing.sm, gap: spacing.xs },
  vinculoTexto: { color: colors.sub, fontSize: fontSize.secondary },
  chipsRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs },
  chip: {
    paddingVertical: spacing.xs + 2,
    paddingHorizontal: spacing.md,
    borderRadius: radius.pill,
    backgroundColor: colors.surface2,
    borderWidth: 1,
    borderColor: colors.line,
  },
  chipLabel: { color: colors.text, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  form: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
