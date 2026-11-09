import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Video } from 'lucide-react-native';
import { StatusChip } from './StatusChip';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

interface Props {
  nome: string;
  aoVivo?: boolean;
  onPress: () => void;
}

/** Sprint 16 (ADR 0019, UX001) — card de preview de câmera com badge AO VIVO (Início, Câmeras). */
export function CameraCard({ nome, aoVivo = true, onPress }: Props) {
  return (
    <Pressable onPress={onPress} style={styles.container}>
      {aoVivo ? (
        <View style={styles.badgeWrap}>
          <StatusChip label="AO VIVO" variant="error" pulse />
        </View>
      ) : null}
      <View style={styles.preview}>
        <Video size={iconSize.lg} color={colors.mute} />
      </View>
      <View style={styles.rodape}>
        <Text style={styles.nome}>{nome}</Text>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    width: 140,
    height: 116,
    borderRadius: radius.lg,
    overflow: 'hidden',
    backgroundColor: colors.surface2,
    borderWidth: 1,
    borderColor: colors.lineSoft,
  },
  badgeWrap: { position: 'absolute', top: spacing.sm, left: spacing.sm, zIndex: 1 },
  preview: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  rodape: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    paddingHorizontal: spacing.sm,
    paddingBottom: spacing.xs,
    paddingTop: spacing.lg,
  },
  nome: { color: colors.text, fontSize: fontSize.meta, fontWeight: fontWeight.medium },
});
