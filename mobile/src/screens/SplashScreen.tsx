import React from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { ShieldCheck } from 'lucide-react-native';
import { colors, fontSize, fontWeight, spacing } from '../theme/theme';

/** Mostrada enquanto o AuthProvider decide se há sessão salva — sem rota própria. */
export function SplashScreen() {
  return (
    <View style={styles.container}>
      <View style={styles.iconWrap}>
        <ShieldCheck size={44} color={colors.safe} />
      </View>
      <Text style={styles.title}>Segurança Conectada</Text>
      <ActivityIndicator color={colors.safe} style={styles.spinner} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.bg,
    alignItems: 'center',
    justifyContent: 'center',
  },
  iconWrap: {
    width: 84,
    height: 84,
    borderRadius: 999,
    backgroundColor: colors.safeDim,
    borderWidth: 1.5,
    borderColor: colors.safe,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.lg,
  },
  title: {
    color: colors.text,
    fontSize: fontSize.title,
    fontWeight: fontWeight.bold,
  },
  spinner: {
    marginTop: spacing.xl,
  },
});
