import React from 'react';
import { Image, Pressable, StyleSheet, Text, View } from 'react-native';
import { Maximize, VideoOff } from 'lucide-react-native';
import type { CameraResponse, StatusCamera } from '../api/types';
import { useImagemAutenticada } from '../api/useImagemAutenticada';
import { colors, efeitos, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';
import { rotuloTimestampCurto } from './cameraLabels';

interface Props {
  camera: CameraResponse;
  onPress: () => void;
}

const STATUS: Record<StatusCamera, { rotulo: string; cor: string }> = {
  Online: { rotulo: 'Online', cor: colors.marca },
  Offline: { rotulo: 'Offline', cor: colors.danger },
  Desconhecido: { rotulo: 'Sem sinal', cor: colors.sub },
};

/**
 * Sprint 20 (ADR 0024) — item do grid de câmeras. Akteck Seg (ADR 0037): visual da referência
 * (imagem com nome e estado sobrepostos, botão de ampliar). Mostra a ÚLTIMA imagem capturada,
 * nunca diz "ao vivo": transmissão contínua ainda não existe (Regra de Vocabulário mantida).
 */
export function CameraCard({ camera, onPress }: Props) {
  const imagem = useImagemAutenticada(camera.ultimaImagemUrl ?? null, camera.ultimaImagemUrl);
  const status = STATUS[camera.status] ?? STATUS.Desconhecido;

  return (
    <Pressable style={styles.container} onPress={onPress} accessibilityRole="button" accessibilityLabel={`${camera.nome}, ${status.rotulo}. Abrir`}>
      <View style={styles.imagemWrap}>
        {imagem ? (
          <Image source={{ uri: imagem }} style={styles.imagem} resizeMode="cover" />
        ) : (
          <View style={styles.semImagem}>
            <VideoOff size={iconSize.lg} color={colors.mute} />
            <Text style={styles.semImagemTexto}>Sem imagem</Text>
          </View>
        )}
        <View style={styles.rodape}>
          <View style={styles.rodapeTexto}>
            <Text style={styles.nome} numberOfLines={1}>
              {camera.nome}
            </Text>
            <View style={styles.statusLinha}>
              <View style={[styles.ponto, { backgroundColor: status.cor }]} />
              <Text style={[styles.status, { color: status.cor }]} numberOfLines={1}>
                {status.rotulo}
              </Text>
            </View>
          </View>
          <Maximize size={iconSize.md} color={colors.text} />
        </View>
      </View>
      <Text style={styles.timestamp} numberOfLines={1}>
        {rotuloTimestampCurto(camera)}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, minWidth: 0 },
  imagemWrap: {
    width: '100%',
    aspectRatio: 1,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    overflow: 'hidden',
    boxShadow: efeitos.brilhoSuave,
  },
  imagem: { width: '100%', height: '100%' },
  semImagem: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: spacing.xs, backgroundColor: colors.surface2 },
  semImagemTexto: { color: colors.mute, fontSize: fontSize.tiny },
  rodape: {
    position: 'absolute',
    left: 0,
    right: 0,
    bottom: 0,
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: spacing.sm,
    padding: spacing.md,
    backgroundColor: 'rgba(3,8,7,0.72)',
  },
  rodapeTexto: { flex: 1, minWidth: 0 },
  nome: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  statusLinha: { flexDirection: 'row', alignItems: 'center', gap: 6, marginTop: 2 },
  ponto: { width: 8, height: 8, borderRadius: 4 },
  status: { fontSize: fontSize.tiny, fontWeight: fontWeight.bold },
  timestamp: { color: colors.mute, fontSize: fontSize.tiny, marginTop: spacing.xs },
});
