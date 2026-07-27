import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Image } from 'expo-image';
import { VideoOff } from 'lucide-react-native';
import { env } from '../config/env';
import { useAuthHeader } from '../api/useAuthHeader';
import { rotuloStatusBadge, rotuloTimestampCurto } from './cameraLabels';
import type { CameraResponse, StatusCamera } from '../api/types';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

interface Props {
  camera: CameraResponse;
  onPress: () => void;
}

/**
 * Sprint 20 (ADR 0024) — item do grid de 2 colunas da aba Câmeras. Nunca mostra
 * termo técnico (sem "snapshot"/"stream"/etc, ver Regra de Vocabulário); "Sem
 * imagem" é a única forma honesta de descrever uma câmera que nunca capturou
 * nada, em vez de fingir uma miniatura que não existe.
 */
export function CameraCard({ camera, onPress }: Props) {
  const authHeader = useAuthHeader();
  const temImagem = !!camera.ultimaImagemUrl && !!authHeader;

  return (
    <Pressable style={styles.container} onPress={onPress}>
      <View style={styles.imagemWrap}>
        {temImagem ? (
          <Image
            source={{ uri: `${env.apiUrl}${camera.ultimaImagemUrl}`, headers: authHeader }}
            style={styles.imagem}
            contentFit="cover"
            transition={200}
            cachePolicy="disk"
          />
        ) : (
          <View style={styles.semImagem}>
            <VideoOff size={iconSize.lg} color={colors.mute} />
          </View>
        )}
        <View style={[styles.badge, badgeStylePorStatus(camera.status)]}>
          <Text style={styles.badgeTexto}>{rotuloStatusBadge(camera.status)}</Text>
        </View>
      </View>
      <Text style={styles.nome} numberOfLines={1}>
        {camera.nome}
      </Text>
      <Text style={styles.timestamp} numberOfLines={1}>
        {rotuloTimestampCurto(camera)}
      </Text>
    </Pressable>
  );
}

function badgeStylePorStatus(status: StatusCamera) {
  if (status === 'Online') return styles.badgeOnline;
  if (status === 'Offline') return styles.badgeOffline;
  return styles.badgeDesconhecido;
}

const styles = StyleSheet.create({
  container: { width: '47%' },
  imagemWrap: {
    width: '100%',
    aspectRatio: 4 / 3,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    overflow: 'hidden',
  },
  imagem: { width: '100%', height: '100%' },
  semImagem: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surface2 },
  badge: {
    position: 'absolute',
    left: spacing.xs,
    bottom: spacing.xs,
    paddingHorizontal: spacing.xs,
    paddingVertical: 2,
    borderRadius: radius.sm,
  },
  badgeOnline: { backgroundColor: colors.safeDim },
  badgeOffline: { backgroundColor: colors.dangerDim },
  badgeDesconhecido: { backgroundColor: colors.surface2 },
  badgeTexto: { color: colors.text, fontSize: fontSize.tiny, fontWeight: fontWeight.medium },
  nome: { color: colors.text, fontSize: fontSize.secondary, fontWeight: fontWeight.bold, marginTop: spacing.xs },
  timestamp: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
});
