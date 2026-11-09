import React from 'react';
import { KeyboardAvoidingView, ScrollView, StyleSheet, type StyleProp, type ViewStyle } from 'react-native';
import { spacing } from '../theme/theme';

/**
 * Formulário que aparece embaixo de uma lista (Visitantes, Portões, Vagas, Entregas…). Antes era
 * uma `View` fixa: em celular pequeno, ou com o teclado aberto, os últimos campos e os botões
 * ficavam fora da tela. Agora rola, sobe junto com o teclado e ocupa no máximo 75% da altura —
 * a lista acima continua visível.
 */
export function FormularioRolavel({ style, children }: { style?: StyleProp<ViewStyle>; children: React.ReactNode }) {
  return (
    <KeyboardAvoidingView behavior="padding" style={styles.envolve}>
      <ScrollView contentContainerStyle={[styles.conteudo, style]} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
        {children}
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  envolve: { flexGrow: 0, flexShrink: 1, maxHeight: '75%' },
  conteudo: { paddingTop: spacing.sm, paddingBottom: spacing.md },
});
