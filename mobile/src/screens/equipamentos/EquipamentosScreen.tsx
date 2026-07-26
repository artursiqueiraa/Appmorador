import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, ChevronRight, Pencil, Router, Trash2, Wifi, WifiOff } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { EquipamentoResponse, FabricanteEquipamento } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { FabricanteEquipamentoSelector, rotuloFabricanteEquipamento } from '../../components/FabricanteEquipamentoSelector';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type EquipamentosRouteProp = RouteProp<RootStackParamList, 'Equipamentos'>;

/**
 * Sprint 11 — Migração da Integração Control iD (ADR 0014). Mesmo padrão de tela de
 * `PontosAcessoScreen` (lista + formulário inline) — Equipamento pertence direto à
 * Propriedade. Ao contrário de Pontos de Acesso, cada card navega para
 * `DetalhesEquipamento` (ações de integração real vivem lá, não aqui).
 */
export function EquipamentosScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<EquipamentosRouteProp>();
  const { propriedadeId, nomePropriedade } = params;

  const [equipamentos, setEquipamentos] = useState<EquipamentoResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<EquipamentoResponse | null>(null);
  const [nome, setNome] = useState('');
  const [modelo, setModelo] = useState('');
  const [fabricante, setFabricante] = useState<FabricanteEquipamento | null>(null);
  const [ip, setIp] = useState('');
  const [porta, setPorta] = useState('');
  const [usuario, setUsuario] = useState('');
  const [senha, setSenha] = useState('');
  const [identificador, setIdentificador] = useState('');
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const lista = await api.get<EquipamentoResponse[]>(`/api/properties/${propriedadeId}/equipamentos`);
      // Centrais JFL/Intelbras têm tela própria — não aparecem aqui para não duplicar o cadastro.
      setEquipamentos(lista.filter((e) => e.fabricante !== 'Jfl' && e.fabricante !== 'Intelbras'));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar os equipamentos.');
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
    setFabricante(null);
    setIp('');
    setPorta('');
    setUsuario('');
    setSenha('');
    setIdentificador('');
    setShowForm(true);
  };

  const abrirEdicao = (equipamento: EquipamentoResponse) => {
    setEditando(equipamento);
    setNome(equipamento.nome);
    setModelo(equipamento.modelo ?? '');
    setFabricante(equipamento.fabricante);
    setIp(equipamento.ip ?? '');
    setPorta(equipamento.porta ? String(equipamento.porta) : '');
    setUsuario(equipamento.usuario ?? '');
    setSenha('');
    setIdentificador(equipamento.identificador ?? '');
    setShowForm(true);
  };

  const formValido = !!nome.trim() && !!fabricante && !!ip.trim() && !!porta.trim() && !!usuario.trim() && (!!editando || !!senha.trim());

  const salvar = async () => {
    if (!formValido || !fabricante) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      const payloadBase = {
        nome: nome.trim(),
        modelo: modelo.trim() || undefined,
        fabricante,
        ip: ip.trim(),
        porta: Number(porta),
        usuario: usuario.trim(),
        identificador: identificador.trim() || undefined,
      };

      if (editando) {
        const atualizado = await api.put<EquipamentoResponse>(`/api/equipamentos/${editando.id}`, {
          ...payloadBase,
          senha: senha.trim() || undefined,
        });
        setEquipamentos((prev) => prev.map((e) => (e.id === atualizado.id ? atualizado : e)));
      } else {
        const criado = await api.post<EquipamentoResponse>(`/api/properties/${propriedadeId}/equipamentos`, {
          ...payloadBase,
          senha: senha.trim(),
        });
        setEquipamentos((prev) => [...prev, criado]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar o equipamento.');
    } finally {
      setSalvando(false);
    }
  };

  const confirmarExclusao = (equipamento: EquipamentoResponse) => {
    Alert.alert('Excluir equipamento?', `"${equipamento.nome}" deixará de aparecer no app.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Excluir',
        style: 'destructive',
        onPress: async () => {
          try {
            await api.delete(`/api/equipamentos/${equipamento.id}`);
            setEquipamentos((prev) => prev.filter((e) => e.id !== equipamento.id));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível excluir o equipamento.');
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
          <Text style={styles.title}>Equipamentos</Text>
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
          data={equipamentos}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => (
            <Pressable
              style={styles.card}
              onPress={() => navigation.navigate('DetalhesEquipamento', { equipamentoId: item.id })}
            >
              <View style={styles.cardMain}>
                <View style={styles.cardIcon}>
                  <Router size={18} color={colors.safe} />
                </View>
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>{item.nome}</Text>
                  <Text style={styles.cardSubtitle}>
                    {rotuloFabricanteEquipamento(item.fabricante)} · {item.ip}:{item.porta}
                  </Text>
                </View>
                {item.status === 'Online' ? (
                  <Wifi size={16} color={colors.safe} />
                ) : (
                  <WifiOff size={16} color={colors.mute} />
                )}
              </View>
              <View style={styles.cardActions}>
                <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar equipamento">
                  <Pencil size={16} color={colors.sub} />
                </Pressable>
                <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir equipamento">
                  <Trash2 size={16} color={colors.danger} />
                </Pressable>
                <ChevronRight size={16} color={colors.mute} />
              </View>
            </Pressable>
          )}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={Router}
                titulo="Nenhum equipamento ainda"
                descricao="Cadastre um controlador de acesso para testar conexão e sincronizar dados reais com ele."
                cta={{ label: 'Adicionar equipamento', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          <TextField label="Nome" value={nome} onChangeText={setNome} placeholder="Ex.: Controlador Portaria" />
          <TextField label="Modelo (opcional)" value={modelo} onChangeText={setModelo} placeholder="Ex.: iDAccess Nano" />
          <FabricanteEquipamentoSelector label="Fabricante" value={fabricante} onChange={setFabricante} />
          <TextField label="IP" value={ip} onChangeText={setIp} placeholder="Ex.: 192.168.1.50" autoCapitalize="none" />
          <TextField label="Porta" value={porta} onChangeText={setPorta} placeholder="Ex.: 80" keyboardType="number-pad" />
          <TextField label="Usuário" value={usuario} onChangeText={setUsuario} placeholder="Usuário de acesso ao equipamento" autoCapitalize="none" />
          <TextField
            label={editando ? 'Senha (deixe em branco para manter a atual)' : 'Senha'}
            value={senha}
            onChangeText={setSenha}
            placeholder="Senha de acesso ao equipamento"
            secureTextEntry
          />
          <TextField label="Identificador (opcional)" value={identificador} onChangeText={setIdentificador} placeholder="Ex.: número de série" />
          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar equipamento'}
            onPress={salvar}
            loading={salvando}
            disabled={!formValido}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : (
        <PrimaryButton label="Adicionar equipamento" variant="secondary" onPress={abrirNovo} />
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
