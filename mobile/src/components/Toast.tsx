import React, { createContext, useCallback, useContext, useRef, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import Animated, { FadeInDown, FadeOutDown } from 'react-native-reanimated';
import { AlertCircle } from 'lucide-react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

interface ToastOptions {
  titulo: string;
  mensagem: string;
  onRetry?: () => void;
}

interface ToastContextValue {
  mostrarErro: (opcoes: ToastOptions) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

const DURACAO_MS = 4000;

/**
 * Sprint 17 (ADR 0020) — feedback de erro para ações pontuais (ex.: "abrir portão"
 * falhou) — diferente do ErrorBoundary (erro de renderização) e do estado `error`
 * de cada tela (erro ao carregar dados). Sempre com "Tentar novamente" quando a ação
 * permitir, nunca com texto técnico (a mensagem já chega amigável via
 * `mapErrorToUserMessage`, ver `api/client.ts`).
 */
export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toast, setToast] = useState<ToastOptions | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const mostrarErro = useCallback((opcoes: ToastOptions) => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    setToast(opcoes);
    timeoutRef.current = setTimeout(() => setToast(null), DURACAO_MS);
  }, []);

  const fechar = () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    setToast(null);
  };

  return (
    <ToastContext.Provider value={{ mostrarErro }}>
      {children}
      {toast ? (
        <Animated.View entering={FadeInDown.duration(250)} exiting={FadeOutDown.duration(250)} style={styles.container}>
          <View style={styles.iconWrap}>
            <AlertCircle size={18} color={colors.danger} />
          </View>
          <View style={styles.textWrap}>
            <Text style={styles.titulo}>{toast.titulo}</Text>
            <Text style={styles.mensagem}>{toast.mensagem}</Text>
            {toast.onRetry ? (
              <Pressable
                onPress={() => {
                  fechar();
                  toast.onRetry?.();
                }}
                style={styles.retryBtn}
              >
                <Text style={styles.retryLabel}>Tentar novamente</Text>
              </Pressable>
            ) : null}
          </View>
        </Animated.View>
      ) : null}
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast precisa ser usado dentro de um ToastProvider.');
  }

  return context;
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    left: spacing.lg,
    right: spacing.lg,
    bottom: spacing.xxl,
    flexDirection: 'row',
    gap: spacing.sm,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.dangerLine,
  },
  iconWrap: { marginTop: 2 },
  textWrap: { flex: 1 },
  titulo: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.bold },
  mensagem: { color: colors.sub, fontSize: fontSize.tiny, marginTop: 2 },
  retryBtn: { marginTop: spacing.sm, alignSelf: 'flex-start' },
  retryLabel: { color: colors.accent, fontSize: fontSize.secondary, fontWeight: fontWeight.bold },
});
