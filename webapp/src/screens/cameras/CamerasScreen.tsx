import React, { useCallback, useEffect, useState } from 'react';
import { FlatList, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Search, Video } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import { usePermissao } from '../../auth/usePermissao';
import { api, ApiError } from '../../api/client';
import type { CameraResponse, StatusCamera } from '../../api/types';
import { useRealtimeCamera } from '../../realtime/RealtimeContext';
import { EstadoVazio } from '../../components/EstadoVazio';
import { PrimaryButton } from '../../components/PrimaryButton';
import { CameraCard } from '../../cameras/CameraCard';
import { TextField } from '../../components/TextField';
import { AbasSegmentadas } from '../../components/neon/AbasSegmentadas';
import { TelaCabecalho } from '../../components/neon/TelaCabecalho';
import { useLayoutDesktop } from '../../layout/useLayoutDesktop';

import { SkeletonCameras } from '../../cameras/SkeletonCameras';
import { aplicarAtualizacaoCamera } from '../../cameras/aplicarAtualizacaoCamera';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

/**
 * Sprint 20 (ADR 0024) — substitui o Empty State genérico "recurso está chegando"
 * (Sprint 16) por uma lista real. Grid de 2 colunas (`FlatList numColumns=2`,
 * mesmo padrão de pull-to-refresh já usado em `EventosScreen`/`AccessScreen`).
 * Status atualiza sozinho via SignalR (`useRealtimeCamera`) — nunca precisa de
 * refresh manual para refletir uma câmera que ficou offline/voltou online
 * enquanto o morador está nesta tela.
 */
export function CamerasScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const { temFeature } = usePermissao();
  const temFeatureCameras = temFeature('Cameras');
  const { ultimaAtualizacaoCamera } = useRealtimeCamera();
  const [cameras, setCameras] = useState<CameraResponse[] | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [atualizando, setAtualizando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [busca, setBusca] = useState('');
  const [filtro, setFiltro] = useState<'Todas' | StatusCamera>('Todas');
  const desktop = useLayoutDesktop();

  const carregar = useCallback(
    async (comIndicadorDeAtualizacao = false) => {
      if (!selectedProperty || !temFeatureCameras) {
        setCarregando(false);
        return;
      }

      if (comIndicadorDeAtualizacao) {
        setAtualizando(true);
      } else {
        setCarregando(true);
      }
      setErro(null);

      try {
        const resposta = await api.get<CameraResponse[]>(`/api/properties/${selectedProperty.id}/cameras`);
        setCameras(resposta);
      } catch (err) {
        setErro(err instanceof ApiError ? err.message : 'Não foi possível carregar as câmeras agora.');
      } finally {
        setCarregando(false);
        setAtualizando(false);
      }
    },
    [selectedProperty, temFeatureCameras],
  );

  useFocusEffect(
    useCallback(() => {
      carregar();
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedProperty?.id]),
  );

  // Sprint 20 (Fase 3/7) — patch parcial (mesma Regra 5 do ADR 0022): só a câmera
  // cujo id bate é atualizada, sem refazer a lista inteira.
  useEffect(() => {
    if (!ultimaAtualizacaoCamera || !selectedProperty || ultimaAtualizacaoCamera.propriedadeId !== selectedProperty.id) {
      return;
    }

    setCameras((atual) => (atual ? aplicarAtualizacaoCamera(atual, ultimaAtualizacaoCamera) : atual));
  }, [ultimaAtualizacaoCamera, selectedProperty]);

  if (carregando) {
    return (
      <ScrollView style={styles.container} contentContainerStyle={styles.content}>
        <CabecalhoCameras />
        <SkeletonCameras />
      </ScrollView>
    );
  }

  if (erro) {
    return (
      <ScrollView style={styles.container} contentContainerStyle={styles.content}>
        <CabecalhoCameras />
        <View style={styles.erroWrap}>
          <Text style={styles.erroTitulo}>Não foi possível carregar</Text>
          <Text style={styles.erroTexto}>{erro}</Text>
          <PrimaryButton label="Tentar novamente" variant="secondary" onPress={() => carregar()} />
        </View>
      </ScrollView>
    );
  }

  // Sprint 21 (ADR 0026) — aba continua sempre visível (ADR 0019, Navegação
  // Previsível): sem a feature contratada, mostra um estado honesto em vez de
  // esconder a aba inteira ou tentar carregar câmeras que não existem.
  if (!temFeatureCameras) {
    return (
      <ScrollView style={styles.container} contentContainerStyle={styles.content}>
        <CabecalhoCameras />
        <EstadoVazio
          icon={Video}
          titulo="Câmeras não contratadas"
          descricao="Esta propriedade ainda não tem câmeras habilitadas. Fale com o administrador para contratar."
        />
      </ScrollView>
    );
  }

  const termo = busca.trim().toLowerCase();
  const lista = (cameras ?? []).filter((c) => (filtro === 'Todas' || c.status === filtro) && (!termo || c.nome.toLowerCase().includes(termo)));
  const colunas = desktop ? 4 : 2;

  return (
    <FlatList
      key={colunas}
      style={styles.container}
      contentContainerStyle={styles.content}
      data={lista}
      keyExtractor={(item) => item.id}
      numColumns={colunas}
      columnWrapperStyle={styles.linha}
      refreshControl={<RefreshControl refreshing={atualizando} onRefresh={() => carregar(true)} tintColor={colors.marca} />}
      ListHeaderComponent={
        <View style={styles.cabecalho}>
          <CabecalhoCameras />
          <TextField label="Buscar" icone={Search} value={busca} onChangeText={setBusca} placeholder="Buscar câmera..." />
          <AbasSegmentadas
            abas={[
              { valor: 'Todas', rotulo: 'Todas', contagem: cameras?.length ?? 0 },
              { valor: 'Online', rotulo: 'Online', contagem: (cameras ?? []).filter((c) => c.status === 'Online').length },
              { valor: 'Offline', rotulo: 'Offline', contagem: (cameras ?? []).filter((c) => c.status === 'Offline').length },
            ]}
            valor={filtro}
            onChange={setFiltro}
          />
        </View>
      }
      ListEmptyComponent={
        <EstadoVazio
          icon={Video}
          titulo={(cameras?.length ?? 0) === 0 ? 'Nenhuma câmera configurada' : 'Nenhuma câmera neste filtro'}
          descricao={(cameras?.length ?? 0) === 0 ? 'Peça ao administrador para adicionar câmeras à sua propriedade.' : 'Tente outro nome ou outro filtro.'}
        />
      }
      renderItem={({ item }) => (
        <CameraCard camera={item} onPress={() => navigation.navigate('DetalheCamera', { cameraId: item.id, nomeCamera: item.nome })} />
      )}
    />
  );
}

/** Akteck Seg (ADR 0037) — cabeçalho da referência. "Última imagem", nunca "ao vivo" (Regra de Vocabulário). */
function CabecalhoCameras() {
  return <TelaCabecalho titulo="Câmeras" subtitulo="Acompanhe as câmeras da sua propriedade." />;
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl, flexGrow: 1 },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold, marginBottom: spacing.lg },
  linha: { gap: spacing.md, marginBottom: spacing.md },
  cabecalho: { gap: spacing.md, marginBottom: spacing.lg },
  erroWrap: {
    padding: spacing.xl,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    gap: spacing.sm,
  },
  erroTitulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  erroTexto: { color: colors.sub, fontSize: fontSize.secondary, textAlign: 'center' },
});
