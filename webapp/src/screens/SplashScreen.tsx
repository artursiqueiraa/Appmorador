import React from 'react';
import { ActivityIndicator, StyleSheet, View } from 'react-native';
import { MarcaAkteck } from '../components/marca/MarcaAkteck';
import { colors, fontSize, fontWeight, spacing } from '../theme/theme';

/** Mostrada enquanto o AuthProvider decide se há sessão salva — sem rota própria. */
export function SplashScreen() {
  return (
    <View style={styles.container}>
      <MarcaAkteck variante="empilhada" tamanho={40} />
      <ActivityIndicator color={colors.marca} style={styles.spinner} />
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
    backgroundColor: colors.marcaDim,
    borderWidth: 1.5,
    borderColor: colors.marca,
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
