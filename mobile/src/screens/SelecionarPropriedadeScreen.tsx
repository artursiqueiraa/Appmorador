import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Home, LogOut, Pencil, Trash2 } from 'lucide-react-native';
import { useAuth } from '../auth/AuthContext';
import type { RootStackParamList } from '../navigation/types';
import { api, ApiError } from '../api/client';
import type { PropriedadeResponse } from '../api/types';
import { PrimaryButton } from '../components/PrimaryButton';
import { TextField } from '../components/TextField';
import { TipoPropriedadeSelector, rotuloTipoPropriedade, type TipoPropriedade } from '../components/TipoPropriedadeSelector';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

/**
 * Sprint 16 (ADR 0019, UX001) — simplificada: cada propriedade agora só tem
 * Editar/Excluir como ações — o resto (unidades, controle de acesso, centrais...)
 * mudou para "Ajustes → Minha Propriedade", alcançável depois de entrar (nunca
 * precisa voltar aqui para configurar nada). Tocar no card inteiro entra na
 * propriedade.
 */
export function SelecionarPropriedadeScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { user, selectProperty, logout } = useAuth();
  const [properties, setProperties] = useState<PropriedadeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<PropriedadeResponse | null>(null);
  const [nome, setNome] = useState('');
  const [tipo, setTipo] = useState<TipoPropriedade | null>(null);
  const [endereco, setEndereco] = useState('');
  const [salvando, setSalvando] = useState(false);

  const loadProperties = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const list = await api.get<PropriedadeResponse[]>('/api/properties');
      setProperties(list);
      setShowForm(list.length === 0);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar suas propriedades.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProperties();
  }, [loadProperties]);

  const abrirNovo = () => {
    setEditando(null);
    setNome('');
    setTipo(null);
    setEndereco('');
    setShowForm(true);
  };

  const abrirEdicao = (propriedade: PropriedadeResponse) => {
    setEditando(propriedade);
    setNome(propriedade.nome);
    setTipo(propriedade.tipo);
    setEndereco(propriedade.endereco ?? '');
    setShowForm(true);
  };

  const salvar = async () => {
    if (!tipo || !nome.trim()) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      if (editando) {
        const atualizada = await api.put<PropriedadeResponse>(`/api/properties/${editando.id}`, {
          nome: nome.trim(),
          tipo,
          endereco: endereco.trim() || undefined,
        });
        setProperties((prev) => prev.map((p) => (p.id === atualizada.id ? atualizada : p)));
      } else {
        const created = await api.post<PropriedadeResponse>('/api/properties', {
          nome: nome.trim(),
          tipo,
          endereco: endereco.trim() || undefined,
        });
        setProperties((prev) => [...prev, created]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar a propriedade.');
    } finally {
      setSalvando(false);
    }
  };

  const confirmarExclusao = (propriedade: PropriedadeResponse) => {
    Alert.alert(
      'Excluir propriedade?',
      `"${propriedade.nome}" e todas as unidades/moradores cadastrados nela deixarão de aparecer no app.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Excluir',
          style: 'destructive',
          onPress: async () => {
            try {
              await api.delete(`/api/properties/${propriedade.id}`);
              setProperties((prev) => prev.filter((p) => p.id !== propriedade.id));
            } catch (err) {
              setError(err instanceof ApiError ? err.message : 'Não foi possível excluir a propriedade.');
            }
          },
        },
      ],
    );
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View>
          <Text style={styles.greeting}>Olá, {user?.nome?.split(' ')[0]}</Text>
          <View style={styles.titleRow}>
            <Text style={styles.title}>Suas propriedades</Text>
            {properties.length > 0 ? (
              <View style={styles.totalBadge}>
                <Text style={styles.totalBadgeLabel}>{properties.length}</Text>
              </View>
            ) : null}
          </View>
        </View>
        <Pressable onPress={logout} style={styles.iconBtn}>
          <LogOut size={18} color={colors.sub} />
        </Pressable>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      <FlatList
        data={properties}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.list}
        refreshing={loading}
        onRefresh={loadProperties}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <Pressable style={styles.cardMain} onPress={() => selectProperty(item)}>
              <View style={styles.cardIcon}>
                <Home size={20} color={colors.safe} />
              </View>
              <View style={styles.cardTextWrap}>
                <Text style={styles.cardTitle}>{item.nome}</Text>
                <View style={styles.cardMetaRow}>
                  <View style={styles.badge}>
                    <Text style={styles.badgeLabel}>{rotuloTipoPropriedade(item.tipo)}</Text>
                  </View>
                  {item.endereco ? <Text style={styles.cardSubtitle}>{item.endereco}</Text> : null}
                </View>
              </View>
            </Pressable>
            <View style={styles.cardActions}>
              <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar propriedade">
                <Pencil size={16} color={colors.sub} />
              </Pressable>
              <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir propriedade">
                <Trash2 size={16} color={colors.danger} />
              </Pressable>
            </View>
          </View>
        )}
        ListEmptyComponent={!loading && !showForm ? <Text style={styles.empty}>Nenhuma propriedade ainda.</Text> : null}
      />

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Nome da propriedade" value={nome} onChangeText={setNome} placeholder="Ex.: Minha casa" />
          <TipoPropriedadeSelector label="Tipo de propriedade" value={tipo} onChange={setTipo} />
          <TextField label="Endereço (opcional)" value={endereco} onChangeText={setEndereco} placeholder="Rua, número" />
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Salvar propriedade'}
            onPress={salvar}
            loading={salvando}
            disabled={!nome || !tipo}
          />
          {properties.length > 0 ? (
            <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
          ) : (
            <PrimaryButton label="Prefiro ser guiado passo a passo" variant="secondary" onPress={() => navigation.navigate('Onboarding')} />
          )}
        </View>
      ) : (
        <PrimaryButton label="Adicionar propriedade" variant="secondary" onPress={abrirNovo} />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: spacing.xl },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: spacing.lg },
  greeting: { color: colors.sub, fontSize: fontSize.secondary },
  titleRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, marginTop: 2 },
  title: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold },
  totalBadge: {
    minWidth: 22,
    height: 22,
    paddingHorizontal: 6,
    borderRadius: radius.pill,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  totalBadgeLabel: { color: colors.sub, fontSize: fontSize.label, fontWeight: fontWeight.bold },
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
  list: { paddingBottom: spacing.lg },
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
    width: 42,
    height: 42,
    borderRadius: radius.md,
    backgroundColor: colors.safeDim,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cardTextWrap: { flex: 1 },
  cardTitle: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  cardMetaRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, marginTop: 2 },
  badge: {
    paddingHorizontal: spacing.xs + 2,
    paddingVertical: 2,
    borderRadius: radius.sm,
    backgroundColor: colors.surface2,
  },
  badgeLabel: { color: colors.sub, fontSize: fontSize.label, fontWeight: fontWeight.medium },
  cardSubtitle: { color: colors.mute, fontSize: fontSize.tiny },
  cardActions: { flexDirection: 'row', gap: spacing.xs, paddingRight: spacing.md },
  actionBtn: { width: 30, height: 30, alignItems: 'center', justifyContent: 'center' },
  empty: { color: colors.mute, textAlign: 'center', marginTop: spacing.xxl },
  form: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
