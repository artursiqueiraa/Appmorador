import React, { useCallback, useEffect, useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { Home, LogOut } from 'lucide-react-native';
import { useAuth } from '../auth/AuthContext';
import { api, ApiError } from '../api/client';
import type { PropriedadeResponse } from '../api/types';
import { PrimaryButton } from '../components/PrimaryButton';
import { TextField } from '../components/TextField';
import { TipoPropriedadeSelector, rotuloTipoPropriedade, type TipoPropriedade } from '../components/TipoPropriedadeSelector';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

export function SelecionarPropriedadeScreen() {
  const { user, selectProperty, logout } = useAuth();
  const [properties, setProperties] = useState<PropriedadeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [nome, setNome] = useState('');
  const [tipo, setTipo] = useState<TipoPropriedade | null>(null);
  const [endereco, setEndereco] = useState('');
  const [creating, setCreating] = useState(false);

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

  const handleCreate = async () => {
    if (!tipo) {
      return;
    }

    setCreating(true);
    setError(null);
    try {
      const created = await api.post<PropriedadeResponse>('/api/properties', {
        nome: nome.trim(),
        tipo,
        endereco: endereco.trim() || undefined,
      });
      setProperties((prev) => [...prev, created]);
      setNome('');
      setTipo(null);
      setEndereco('');
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível criar a propriedade.');
    } finally {
      setCreating(false);
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View>
          <Text style={styles.greeting}>Olá, {user?.nome?.split(' ')[0]}</Text>
          <Text style={styles.title}>Suas propriedades</Text>
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
          <Pressable style={styles.card} onPress={() => selectProperty(item)}>
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
        )}
        ListEmptyComponent={!loading && !showForm ? <Text style={styles.empty}>Nenhuma propriedade ainda.</Text> : null}
      />

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Nome da propriedade" value={nome} onChangeText={setNome} placeholder="Ex.: Minha casa" />
          <TipoPropriedadeSelector label="Tipo de propriedade" value={tipo} onChange={setTipo} />
          <TextField label="Endereço (opcional)" value={endereco} onChangeText={setEndereco} placeholder="Rua, número" />
          <PrimaryButton label="Salvar propriedade" onPress={handleCreate} loading={creating} disabled={!nome || !tipo} />
          {properties.length > 0 ? (
            <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
          ) : null}
        </View>
      ) : (
        <PrimaryButton label="Adicionar propriedade" variant="secondary" onPress={() => setShowForm(true)} />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: spacing.xl },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: spacing.lg },
  greeting: { color: colors.sub, fontSize: fontSize.secondary },
  title: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold, marginTop: 2 },
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
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
  },
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
  empty: { color: colors.mute, textAlign: 'center', marginTop: spacing.xxl },
  form: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
