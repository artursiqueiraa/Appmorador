import React from 'react';
import { StyleSheet, View } from 'react-native';
import { Skeleton } from '../../components/Skeleton';
import { radius, spacing } from '../../theme/theme';

/** Composição do Skeleton genérico no formato de uma linha da timeline — usado só no primeiro load. */
export function SkeletonEventos() {
  return (
    <View style={styles.container}>
      <Skeleton height={44} radius={radius.md} style={styles.filtro} />
      {[0, 1, 2, 3, 4].map((i) => (
        <View key={i} style={styles.linha}>
          <Skeleton width={38} height={38} radius={999} />
          <View style={styles.textos}>
            <Skeleton width="70%" height={14} />
            <Skeleton width="40%" height={11} style={{ marginTop: 6 }} />
          </View>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { padding: spacing.xl },
  filtro: { marginBottom: spacing.md },
  linha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    marginBottom: spacing.sm,
  },
  textos: { flex: 1, gap: 4 },
});