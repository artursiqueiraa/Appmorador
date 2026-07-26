import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Image, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import * as ImagePicker from 'expo-image-picker';
import { Camera, ChevronLeft, ChevronRight, KeyRound, Trash2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { CredencialResponse, StatusCredencial, TipoCredencial } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TipoCredencialSelector, rotuloTipoCredencial } from '../../components/TipoCredencialSelector';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { obterFotoLocal, removerFotoLocal, salvarFotoLocal } from '../../credenciais/fotoFacialLocal';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

/**
 * Sprint 17 (ADR 0020) — prioridade de captura definida na Discovery: 1) fluxo do
 * equipamento/Control iD (não existe — nenhuma tela/endpoint de "capturar no
 * dispositivo" hoje, dívida técnica); 2) câmera; 3) galeria. `expo-image-picker`
 * cobre tanto câmera (`launchCameraAsync`) quanto galeria (`launchImageLibraryAsync`)
 * sozinho — instalar também `expo-camera` só para a mesma captura de câmera seria
 * uma dependência nativa nova sem ganho real (mission diretriz: reduzir fricção, não
 * inflar risco de engenharia). Se nenhuma das duas permissões for concedida, cai no
 * item 4: aviso de indisponibilidade.
 */
async function capturarFotoFacial(): Promise<string | null> {
  const permissaoCamera = await ImagePicker.requestCameraPermissionsAsync();
  if (permissaoCamera.granted) {
    const resultado = await ImagePicker.launchCameraAsync({ quality: 0.5, allowsEditing: true });
    if (!resultado.canceled) {
      return resultado.assets[0].uri;
    }
    return null;
  }

  const permissaoGaleria = await ImagePicker.requestMediaLibraryPermissionsAsync();
  if (permissaoGaleria.granted) {
    const resultado = await ImagePicker.launchImageLibraryAsync({ quality: 0.5, allowsEditing: true });
    if (!resultado.canceled) {
      return resultado.assets[0].uri;
    }
    return null;
  }

  throw new Error('SEM_PERMISSAO');
}

type CredenciaisRouteProp = RouteProp<RootStackParamList, 'Credenciais'>;

const STATUS_COR: Record<StatusCredencial, string> = {
  Ativa: colors.safe,
  Suspensa: colors.accent,
  Expirada: colors.mute,
  Revogada: colors.danger,
};

const TODOS_STATUS: StatusCredencial[] = ['Ativa', 'Suspensa', 'Expirada', 'Revogada'];

/**
 * Sprint 7 — Controle de Acesso. Mesmo padrão de tela de `UnidadesScreen` (lista +
 * formulário inline). Tipo é imutável após a criação — só o Status muda (ver
 * `alterarStatus`), sempre por confirmação explícita porque uma das opções é
 * "Revogada" (requisito de UX da Sprint: confirmação antes de revogar).
 */
export function CredenciaisScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<CredenciaisRouteProp>();
  const { moradorId, nomeMorador, propriedadeId } = params;

  const [credenciais, setCredenciais] = useState<CredencialResponse[]>([]);
  const [fotosLocais, setFotosLocais] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [tipo, setTipo] = useState<TipoCredencial | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [fotoCapturada, setFotoCapturada] = useState<string | null>(null);
  const [atualizandoFotoDe, setAtualizandoFotoDe] = useState<CredencialResponse | null>(null);

  const credencialFacialExistente = credenciais.find((c) => c.tipo === 'Facial');

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const lista = await api.get<CredencialResponse[]>(`/api/moradores/${moradorId}/credenciais`);
      setCredenciais(lista);

      const fotos = await Promise.all(
        lista.filter((c) => c.tipo === 'Facial').map(async (c) => [c.id, await obterFotoLocal(c.id)] as const),
      );
      setFotosLocais(Object.fromEntries(fotos.filter(([, uri]) => uri !== null)) as Record<string, string>);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar as credenciais.');
    } finally {
      setLoading(false);
    }
  }, [moradorId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const excluirCredencial = async (credencial: CredencialResponse) => {
    try {
      await api.delete(`/api/credenciais/${credencial.id}`);
      await removerFotoLocal(credencial.id);
      setCredenciais((prev) => prev.filter((c) => c.id !== credencial.id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível excluir a credencial.');
    }
  };

  const iniciarCapturaFacial = async (credencialParaAtualizar: CredencialResponse | null) => {
    setError(null);
    try {
      const uri = await capturarFotoFacial();
      if (!uri) {
        return;
      }
      setFotoCapturada(uri);
      setAtualizandoFotoDe(credencialParaAtualizar);
    } catch {
      setError('Cadastro facial ainda não disponível neste ambiente.');
    }
  };

  const cancelarCapturaFacial = () => {
    setFotoCapturada(null);
    setAtualizandoFotoDe(null);
  };

  const confirmarCapturaFacial = async () => {
    if (!fotoCapturada) {
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      if (atualizandoFotoDe) {
        await salvarFotoLocal(atualizandoFotoDe.id, fotoCapturada);
        setFotosLocais((prev) => ({ ...prev, [atualizandoFotoDe.id]: fotoCapturada }));
      } else {
        const criada = await api.post<CredencialResponse>(`/api/moradores/${moradorId}/credenciais`, { tipo: 'Facial' });
        await salvarFotoLocal(criada.id, fotoCapturada);
        setCredenciais((prev) => [...prev, criada]);
        setFotosLocais((prev) => ({ ...prev, [criada.id]: fotoCapturada }));
      }
      setShowForm(false);
      setTipo(null);
      cancelarCapturaFacial();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível criar a credencial.');
    } finally {
      setSalvando(false);
    }
  };

  const gerenciarCredencialFacialExistente = () => {
    if (!credencialFacialExistente) {
      return;
    }

    Alert.alert('Você já possui uma credencial facial', 'O que você quer fazer?', [
      { text: 'Atualizar foto', onPress: () => iniciarCapturaFacial(credencialFacialExistente) },
      { text: 'Remover', style: 'destructive', onPress: () => excluirCredencial(credencialFacialExistente) },
      { text: 'Cancelar', style: 'cancel' },
    ]);
  };

  const salvar = async () => {
    if (!tipo) {
      return;
    }

    if (tipo === 'Facial') {
      if (credencialFacialExistente) {
        gerenciarCredencialFacialExistente();
        return;
      }
      await iniciarCapturaFacial(null);
      return;
    }

    setSalvando(true);
    setError(null);
    try {
      const criada = await api.post<CredencialResponse>(`/api/moradores/${moradorId}/credenciais`, { tipo });
      setCredenciais((prev) => [...prev, criada]);
      setShowForm(false);
      setTipo(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível criar a credencial.');
    } finally {
      setSalvando(false);
    }
  };

  const aplicarStatus = async (credencial: CredencialResponse, novoStatus: StatusCredencial) => {
    try {
      const atualizada = await api.put<CredencialResponse>(`/api/credenciais/${credencial.id}/status`, { status: novoStatus });
      setCredenciais((prev) => prev.map((c) => (c.id === atualizada.id ? atualizada : c)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível atualizar o status.');
    }
  };

  const alterarStatus = (credencial: CredencialResponse) => {
    const opcoes = TODOS_STATUS.filter((s) => s !== credencial.status);
    Alert.alert(
      'Alterar status da credencial',
      `${rotuloTipoCredencial(credencial.tipo)} está ${credencial.status.toLowerCase()}. Selecione o novo status:`,
      [
        ...opcoes.map((status) => ({
          text: status,
          style: status === 'Revogada' ? ('destructive' as const) : ('default' as const),
          onPress: () => aplicarStatus(credencial, status),
        })),
        { text: 'Cancelar', style: 'cancel' as const },
      ],
    );
  };

  const confirmarExclusao = (credencial: CredencialResponse) => {
    Alert.alert(
      'Excluir credencial?',
      `A credencial "${rotuloTipoCredencial(credencial.tipo)}" e suas permissões de acesso deixarão de aparecer no app.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        { text: 'Excluir', style: 'destructive', onPress: () => excluirCredencial(credencial) },
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
          <Text style={styles.title}>Credenciais</Text>
          <Text style={styles.subtitle}>{nomeMorador}</Text>
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
          data={credenciais}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => (
            <View style={styles.card}>
              <Pressable
                style={styles.cardMain}
                onPress={() =>
                  navigation.navigate('Permissoes', {
                    credencialId: item.id,
                    tituloCredencial: rotuloTipoCredencial(item.tipo),
                    propriedadeId,
                  })
                }
              >
                {item.tipo === 'Facial' && fotosLocais[item.id] ? (
                  <Image source={{ uri: fotosLocais[item.id] }} style={styles.cardThumb} />
                ) : (
                  <View style={styles.cardIcon}>
                    <KeyRound size={18} color={STATUS_COR[item.status]} />
                  </View>
                )}
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>{rotuloTipoCredencial(item.tipo)}</Text>
                  <Text style={styles.cardSubtitle}>Toque para ver as permissões de acesso</Text>
                </View>
                <ChevronRight size={18} color={colors.mute} />
              </Pressable>
              <Pressable onPress={() => alterarStatus(item)} style={styles.statusBadge} accessibilityLabel="Alterar status">
                <Text style={[styles.statusLabel, { color: STATUS_COR[item.status] }]}>{item.status}</Text>
              </Pressable>
              <View style={styles.cardActions}>
                {item.tipo === 'Facial' ? (
                  <Pressable
                    onPress={() => iniciarCapturaFacial(item)}
                    style={styles.actionBtn}
                    accessibilityLabel="Atualizar foto"
                  >
                    <Camera size={16} color={colors.accent} />
                  </Pressable>
                ) : null}
                <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Excluir credencial">
                  <Trash2 size={16} color={colors.danger} />
                </Pressable>
              </View>
            </View>
          )}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={KeyRound}
                titulo="Nenhuma credencial ainda"
                descricao="Cadastre a primeira credencial deste morador para liberar o acesso a pontos da propriedade."
                cta={{ label: 'Adicionar credencial', onPress: () => setShowForm(true) }}
              />
            ) : null
          }
        />
      )}

      {fotoCapturada ? (
        <View style={styles.form}>
          <Image source={{ uri: fotoCapturada }} style={styles.preview} />
          <Text style={styles.previewAviso}>
            A foto é usada só para pré-visualização agora. O armazenamento seguro dela será adicionado em uma atualização
            futura.
          </Text>
          <PrimaryButton
            label={atualizandoFotoDe ? 'Confirmar atualização' : 'Confirmar cadastro'}
            onPress={confirmarCapturaFacial}
            loading={salvando}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={cancelarCapturaFacial} />
        </View>
      ) : showForm ? (
        <View style={styles.form}>
          <TipoCredencialSelector label="Tipo de credencial" value={tipo} onChange={setTipo} />
          <PrimaryButton label="Adicionar credencial" onPress={salvar} loading={salvando} disabled={!tipo} />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : (
        <PrimaryButton label="Adicionar credencial" variant="secondary" onPress={() => setShowForm(true)} />
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
  cardThumb: { width: 38, height: 38, borderRadius: radius.md, backgroundColor: colors.surface2 },
  preview: { width: '100%', height: 220, borderRadius: radius.lg, backgroundColor: colors.surface2, marginBottom: spacing.sm },
  previewAviso: { color: colors.sub, fontSize: fontSize.tiny, marginBottom: spacing.md, textAlign: 'center' },
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
  statusBadge: { paddingHorizontal: spacing.xs, paddingVertical: 4 },
  statusLabel: { fontSize: fontSize.label, fontWeight: fontWeight.medium },
  cardActions: { flexDirection: 'row', gap: spacing.xs },
  actionBtn: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  form: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
