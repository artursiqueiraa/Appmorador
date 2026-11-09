import React from 'react';
import { Image, Pressable, StyleSheet, Text, View } from 'react-native';
import { Video } from 'lucide-react-native';
import type { CameraResponse } from '../api/types';
import { useImagemAutenticada } from '../api/useImagemAutenticada';
import { colors, fontSize, fontWeight, radius } from '../theme/theme';

/**
 * Akteck Seg (ADR 0038) — miniatura da fileira de câmeras do Início (protótipo). Mostra a
 * última imagem capturada e o estado Online/Offline; nunca "ao vivo" (Regra de Vocabulário).
 */
export function CameraMini({ camera, onPress }: { camera: CameraResponse; onPress: () => void }) {
  const imagem = useImagemAutenticada(camera.ultimaImagemUrl ?? null, camera.ultimaImagemUrl);
  const online = camera.status === 'Online';
  return (
    <Pressable onPress={onPress} style={styles.cartao} accessibilityRole="button" accessibilityLabel={`Câmera ${camera.nome}, ${online ? 'online' : 'offline'}`}>
      {imagem ? <Image source={{ uri: imagem }} style={StyleSheet.absoluteFill} resizeMode="cover" /> : <View style={styles.semImagem} />}
      <View style={styles.selo}>
        <View style={[styles.ponto, { backgroundColor: online ? colors.safe : colors.danger }]} />
        <Text style={styles.seloTexto}>{online ? 'ONLINE' : 'OFFLINE'}</Text>
      </View>
      {imagem ? null : (
        <View style={styles.icone}>
          <Video size={20} color="rgba(255,255,255,0.35)" />
        </View>
      )}
      <View style={styles.rodape}>
        <Text style={styles.nome} numberOfLines={1}>
          {camera.nome}
        </Text>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  cartao: { width: 150, height: 100, borderRadius: radius.lg, overflow: 'hidden', borderWidth: 1, borderColor: colors.lineSoft, backgroundColor: colors.surface2 },
  semImagem: { position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, backgroundColor: colors.surface2 },
  selo: { position: 'absolute', top: 8, left: 8, flexDirection: 'row', alignItems: 'center', gap: 5 },
  ponto: { width: 6, height: 6, borderRadius: 3 },
  seloTexto: { color: colors.text, fontSize: 10, fontWeight: fontWeight.bold, letterSpacing: 0.5 },
  icone: { position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, alignItems: 'center', justifyContent: 'center' },
  rodape: { position: 'absolute', left: 0, right: 0, bottom: 0, paddingHorizontal: 10, paddingTop: 14, paddingBottom: 8, backgroundColor: 'rgba(0,0,0,0.5)' },
  nome: { color: colors.text, fontSize: fontSize.meta, fontWeight: fontWeight.bold },
});
