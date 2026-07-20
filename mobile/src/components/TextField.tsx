import React from 'react';
import { StyleSheet, Text, TextInput, type TextInputProps, View } from 'react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

interface Props extends TextInputProps {
  label: string;
  error?: string;
}

export function TextField({ label, error, style, ...rest }: Props) {
  return (
    <View style={styles.container}>
      <Text style={styles.label}>{label}</Text>
      <TextInput placeholderTextColor={colors.mute} style={[styles.input, error ? styles.inputError : null, style]} {...rest} />
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginBottom: spacing.lg,
  },
  label: {
    color: colors.sub,
    fontSize: fontSize.meta,
    fontWeight: fontWeight.medium,
    marginBottom: spacing.xs + 2,
  },
  input: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    borderRadius: radius.md,
    paddingHorizontal: spacing.md,
    paddingVertical: 12,
    color: colors.text,
    fontSize: fontSize.body,
  },
  inputError: {
    borderColor: colors.dangerLine,
  },
  error: {
    color: colors.danger,
    fontSize: fontSize.tiny,
    marginTop: spacing.xs,
  },
});
