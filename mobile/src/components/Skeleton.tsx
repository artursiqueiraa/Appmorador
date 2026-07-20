import React, { useEffect } from 'react';
import { StyleSheet, type DimensionValue } from 'react-native';
import Animated, { useAnimatedStyle, useSharedValue, withRepeat, withTiming } from 'react-native-reanimated';
import { colors, motion, radius } from '../theme/theme';

interface Props {
  width?: DimensionValue;
  height?: number;
  radius?: number;
  style?: object;
}

/**
 * Placeholder de carregamento com shimmer — comunica "isso ainda está chegando",
 * nunca decorativo. Primitivo genérico: qualquer tela pode usar, não só o Dashboard.
 */
export function Skeleton({ width = '100%', height = 16, radius: cornerRadius = radius.sm, style }: Props) {
  const opacity = useSharedValue(0.4);

  useEffect(() => {
    opacity.value = withRepeat(withTiming(1, { duration: motion.duration.slow }), -1, true);
  }, [opacity]);

  const animatedStyle = useAnimatedStyle(() => ({ opacity: opacity.value }));

  return (
    <Animated.View
      style={[styles.base, { width, height, borderRadius: cornerRadius }, animatedStyle, style]}
    />
  );
}

const styles = StyleSheet.create({
  base: {
    backgroundColor: colors.surface2,
  },
});
