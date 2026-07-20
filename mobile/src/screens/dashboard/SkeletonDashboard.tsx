import React from 'react';
import { StyleSheet, View } from 'react-native';
import { Skeleton } from '../../components/Skeleton';
import { radius, spacing } from '../../theme/theme';

/** Composição do Skeleton genérico no formato do Dashboard real — usado só no primeiro load. */
export function SkeletonDashboard() {
  return (
    <View style={styles.container}>
      <View style={styles.headerRow}>
        <View style={{ gap: spacing.xs }}>
          <Skeleton width={90} height={12} />
          <Skeleton width={160} height={20} />
        </View>
        <Skeleton width={38} height={38} radius={radius.md} />
      </View>

      <Skeleton height={84} radius={radius.xl} style={styles.block} />
      <Skeleton height={120} radius={radius.xl} style={styles.block} />
      <Skeleton height={72} radius={radius.xl} style={styles.block} />
      <View style={styles.row}>
        <Skeleton height={64} radius={radius.lg} style={{ flex: 1 }} />
        <Skeleton height={64} radius={radius.lg} style={{ flex: 1 }} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { padding: spacing.xl },
  headerRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: spacing.lg },
  block: { marginBottom: spacing.md },
  row: { flexDirection: 'row', gap: spacing.sm },
});
