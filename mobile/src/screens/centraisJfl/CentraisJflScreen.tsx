import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, ChevronRight, Pencil, ShieldCheck, Trash2, Wifi, WifiOff } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { EquipamentoResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type CentraisJflRouteProp = RouteProp<RootStackParamList, 'CentraisJfl'>;

/**
 * Sprint 12 — Migração JFL Active 100 Bus (ADR 0015). Lista/cadastra centrais JFL
 * usando o mesmo Equipamento genérico da Sprint 11 (Fabricante fixo em "Jfl"), mas
 * com um formulário reduzido: a central sempre disca para o AppMorador (nunca o
 * contrário), então só Nome/Modelo/Número de série fazem sentido aqui — sem
 * IP/Porta/Usuário/Senha (ver ADR 0015).
 */
export function CentraisJflScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<CentraisJflRouteProp>();
  const { propriedadeId, nomePropriedade } = params;

  const [centrais, setCentrais] = useState<EquipamentoResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<EquipamentoResponse | null>(null);
  const [nome, setNome] = useState('');
  const [modelo, setModelo] = useState('');
  const [numeroSerie, setNumeroSerie] = useState('');
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const lista = await api.get<EquipamentoResponse[]>(`/api/properties/${propriedadeId}/equipamentos`);
      setCentrais(lista.filter((e) => e.fabricante === 'Jfl'));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar as centrais JFL.');
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
    setModelo('');
    setNumeroSerie('');
    setShowForm(true);
  };

  const abrirEdicao = (central: EquipamentoResponse) => {
    setEditando(central);
    setNome(central.nome);
    setModelo(central.modelo ?? '');
    setNumeroSerie(central.identificador ?? '');
    setShowForm(true);
  };

  const formValido = !!nome.trim() && !!numeroSerie.trim();

  const salvar = async () => {
    if (!formValido) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      const payload = {
        nome: nome.trim(),
        modelo: modelo.trim() || undefined,
        fabricante: 'Jfl' as const,
        identificador: numeroSerie.trim(),
      };

      if (editando) {
        const atualizada = await api.put<EquipamentoResponse>(`/api/equipamentos/${editando.id}`, payload);
        setCentrais((prev) => prev.map((c) => (c.id === atualizada.id ? atualizada : c)));
      } else {
        const criada = await api.post<EquipamentoResponse>(`/api/properties/${propriedadeId}/equipamentos`, payload);
        setCentrais((prev) => [...prev, criada]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar a central JFL.');
    } finally {
      setSalvando(false);
    }
  };

  const confirmarExclusao = (central: EquipamentoResponse) => {
    Alert.alert('Excluir central?', `"${central.nome}" deixará de aparecer no app.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Excluir',
        style: 'destructive',
        onPress: async () => {
          try {
            await api.delete(`/api/equipamentos/${central.id}`);
            setCentrais((prev) => prev.filter((c) => c.id !== central.id));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível excluir a central.');
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
          <Text style={styles.title}>Centrais JFL</Text>
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
          data={centrais}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => (
            <Pressable style={styles.card} onPress={() => navigation.navigate('DetalhesCentralJfl', { equipamentoId: item.id })}>
              <View style={styles.cardMain}>
                <View style={styles.cardIcon}>
                  <ShieldCheck size={18} color={colors.safe} />
                </View>
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>{item.nome}</Text>
                  <Text style={styles.cardSubtitle}>Nº série {item.identificador}</Text>
                </View>
                {item.status === 'Online' ? <Wifi size={16} color={colors.safe} /> : <WifiOff size={16} color={colors.mute} />}
              </View>
              <View style={styles.cardActions}>
                <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar central">
                  <Pencil size={16} color={colors.sub} />
                </Pressable>
                <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir central">
                  <Trash2 size={16} color={colors.danger} />
                </Pressable>
                <ChevronRight size={16} color={colors.mute} />
              </View>
            </Pressable>
          )}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={ShieldCheck}
                titulo="Nenhuma central JFL ainda"
                descricao="Cadastre a central de alarme para armar, desarmar e acompanhar o status em tempo real."
                cta={{ label: 'Adicionar central JFL', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Nome" value={nome} onChangeText={setNome} placeholder="Ex.: Central Alarme Casa" />
          <TextField label="Modelo (opcional)" value={modelo} onChangeText={setModelo} placeholder="Ex.: Active 100 Bus" />
          <TextField
            label="Número de série"
            value={numeroSerie}
            onChangeText={setNumeroSerie}
            placeholder="Número de série informado pela central"
            autoCapitalize="none"
          />
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar central'}
            onPress={salvar}
            loading={salvando}
            disabled={!formValido}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : (
        <PrimaryButton label="Adicionar central" variant="secondary" onPress={abrirNovo} />
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
  cardActions: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, paddingRight: spacing.md },
  actionBtn: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  form: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
