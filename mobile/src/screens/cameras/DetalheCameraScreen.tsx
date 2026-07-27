import React, { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Image } from 'expo-image';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Animated, { FadeIn } from 'react-native-reanimated';
import { ChevronLeft, RefreshCw, VideoOff } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import { useAuthHeader } from '../../api/useAuthHeader';
import { env } from '../../config/env';
import type { CameraSnapshotResponse } from '../../api/types';
import { useRealtimeCamera } from '../../realtime/RealtimeContext';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { rotuloStatusDetalhado } from '../../cameras/cameraLabels';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

type DetalheCameraRouteProp = RouteProp<RootStackParamList, 'DetalheCamera'>;

/**
 * Sprint 20 (ADR 0024) — imagem ampliada + "Atualizar imagem" (dispara uma captura
 * nova sob demanda, 2-10s de latência real ao gravador). Falha nunca troca a tela
 * inteira por um erro — mantém a última imagem disponível visível, com um aviso
 * amigável logo abaixo (mesmo racional do Painel de Controle da Sprint 18: nunca
 * um estado "quebrado" quando ainda há algo útil para mostrar).
 */
export function DetalheCameraScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const route = useRoute<DetalheCameraRouteProp>();
  const { cameraId, nomeCamera } = route.params;
  const insets = useSafeAreaInsets();
  const authHeader = useAuthHeader();
  const { ultimaAtualizacaoCamera } = useRealtimeCamera();

  const [snapshot, setSnapshot] = useState<CameraSnapshotResponse | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [atualizando, setAtualizando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [avisoFalhaAtualizacao, setAvisoFalhaAtualizacao] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setCarregando(true);
    setErro(null);
    try {
      const resposta = await api.get<CameraSnapshotResponse | undefined>(`/api/cameras/${cameraId}/snapshot`);
      setSnapshot(resposta ?? null);
    } catch (err) {
      setErro(err instanceof ApiError ? err.message : 'Não foi possível carregar esta câmera agora.');
    } finally {
      setCarregando(false);
    }
  }, [cameraId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  // Sprint 20 (Fase 7) — se a câmera mudar de status enquanto o morador está com o
  // detalhe aberto (ex.: voltou online por uma captura disparada por alarme em
  // outro fluxo), a tela atualiza sozinha via SignalR.
  useEffect(() => {
    if (!ultimaAtualizacaoCamera || ultimaAtualizacaoCamera.cameraId !== cameraId) {
      return;
    }
    setSnapshot((atual) => ({
      sucesso: true,
      status: ultimaAtualizacaoCamera.status,
      ultimaImagemUrl: ultimaAtualizacaoCamera.ultimaImagemUrl ?? atual?.ultimaImagemUrl ?? null,
      capturadaEmUtc: ultimaAtualizacaoCamera.ultimaAtualizacaoUtc ?? atual?.capturadaEmUtc ?? null,
      mensagemErro: null,
    }));
  }, [ultimaAtualizacaoCamera, cameraId]);

  const atualizarImagem = async () => {
    setAtualizando(true);
    setAvisoFalhaAtualizacao(null);
    try {
      const resposta = await api.post<CameraSnapshotResponse>(`/api/cameras/${cameraId}/snapshot`);
      setSnapshot(resposta);
      if (!resposta.sucesso) {
        setAvisoFalhaAtualizacao(resposta.mensagemErro ?? 'Não foi possível atualizar a imagem agora.');
      }
    } catch (err) {
      setAvisoFalhaAtualizacao(err instanceof ApiError ? err.message : 'Não foi possível atualizar a imagem agora.');
    } finally {
      setAtualizando(false);
    }
  };

  const status = snapshot?.status ?? 'Desconhecido';
  const temImagem = !!snapshot?.ultimaImagemUrl && !!authHeader;

  return (
    <ScrollView style={styles.container} contentContainerStyle={[styles.content, { paddingTop: insets.top + spacing.md }]}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <Text style={styles.titulo} numberOfLines={1}>
          {nomeCamera}
        </Text>
      </View>

      {carregando ? (
        <Skeleton height={260} radius={radius.xl} />
      ) : erro ? (
        <View style={styles.erroWrap}>
          <Text style={styles.erroTexto}>{erro}</Text>
          <PrimaryButton label="Tentar novamente" variant="secondary" onPress={carregar} />
        </View>
      ) : (
        <Animated.View entering={FadeIn.duration(250)} style={styles.imagemWrap}>
          {temImagem ? (
            <Image
              source={{ uri: `${env.apiUrl}${snapshot!.ultimaImagemUrl}`, headers: authHeader }}
              style={styles.imagem}
              contentFit="cover"
              transition={200}
              cachePolicy="disk"
            />
          ) : (
            <View style={styles.semImagem}>
              <VideoOff size={iconSize.xl} color={colors.mute} />
              <Text style={styles.semImagemTexto}>Nenhuma imagem disponível</Text>
            </View>
          )}
        </Animated.View>
      )}

      {!carregando && !erro ? (
        <>
          <Text style={styles.status}>{rotuloStatusDetalhado(status, snapshot?.capturadaEmUtc)}</Text>

          {avisoFalhaAtualizacao ? <Text style={styles.aviso}>{avisoFalhaAtualizacao}</Text> : null}

          <Pressable style={[styles.botaoAtualizar, atualizando && styles.botaoDesabilitado]} onPress={atualizarImagem} disabled={atualizando}>
            {atualizando ? (
              <ActivityIndicator color={colors.bg} size="small" />
            ) : (
              <>
                <RefreshCw size={iconSize.sm} color={colors.bg} />
                <Text style={styles.botaoAtualizarTexto}>Atualizar imagem</Text>
              </>
            )}
          </Pressable>
        </>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.lg },
  iconBtn: {
    width: 36,
    height: 36,
    borderRadius: radius.sm,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { flex: 1, color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  imagemWrap: {
    width: '100%',
    aspectRatio: 4 / 3,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    overflow: 'hidden',
  },
  imagem: { width: '100%', height: '100%' },
  semImagem: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: spacing.sm, backgroundColor: colors.surface2 },
  semImagemTexto: { color: colors.mute, fontSize: fontSize.secondary },
  status: { color: colors.sub, fontSize: fontSize.body, marginTop: spacing.md },
  aviso: { color: colors.warn, fontSize: fontSize.secondary, marginTop: spacing.sm },
  botaoAtualizar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    marginTop: spacing.lg,
    paddingVertical: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.accent,
  },
  botaoAtualizarTexto: { color: colors.bg, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  botaoDesabilitado: { opacity: 0.6 },
  erroWrap: {
    padding: spacing.xl,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    gap: spacing.sm,
  },
  erroTexto: { color: colors.sub, fontSize: fontSize.secondary, textAlign: 'center' },
});
