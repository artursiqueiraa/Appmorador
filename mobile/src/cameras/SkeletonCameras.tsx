import React from 'react';
import { StyleSheet, View } from 'react-native';
import { Skeleton } from '../components/Skeleton';
import { radius, spacing } from '../theme/theme';

/** Sprint 20 (ADR 0024) — composição do `Skeleton` genérico, formato de grid 2 colunas (mesmo padrão de `SkeletonEventos.tsx`). */
export function SkeletonCameras() {
  return (
    <View style={styles.grid}>
      {[0, 1, 2, 3].map((i) => (
        <View key={i} style={styles.item}>
          <Skeleton height={140} radius={radius.lg} />
          <Skeleton height={14} width="70%" style={styles.linha} />
          <Skeleton height={11} width="45%" style={styles.linha} />
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.md },
  item: { width: '47%' },
  linha: { marginTop: spacing.xs },
});
