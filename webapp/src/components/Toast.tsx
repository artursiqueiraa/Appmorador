import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import Animated, { FadeInDown, FadeOutDown } from 'react-native-reanimated';
import { AlertCircle, Bell, CheckCircle2, TriangleAlert } from 'lucide-react-native';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

export type TipoToast = 'erro' | 'sucesso' | 'info' | 'alerta';

interface ToastOptions {
  tipo?: TipoToast;
  titulo: string;
  mensagem: string;
  onRetry?: () => void;
}

interface ToastInterno extends ToastOptions {
  id: number;
}

interface ToastContextValue {
  /** Sprint 17 (ADR 0020) — conveniência para o caso mais comum (falha de ação), mantida por compatibilidade. */
  mostrarErro: (opcoes: Omit<ToastOptions, 'tipo'>) => void;
  /** Sprint 18 (ADR 0022, Fase 3) — toast genérico ("✓ Portão aberto", "🔔 Visitante autorizado", "⚠ Alarme disparado"). */
  mostrarToast: (opcoes: ToastOptions) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

const DURACAO_MS = 4000;
/** Sprint 18 (ADR 0022, Regra 4 — Política de Cache) — no máximo 10 toasts pendentes; excedente descarta os mais antigos. */
const FILA_MAXIMA = 10;

const VISUAL_POR_TIPO: Record<TipoToast, { Icone: typeof AlertCircle; cor: string; corLinha: string }> = {
  erro: { Icone: AlertCircle, cor: colors.danger, corLinha: colors.dangerLine },
  alerta: { Icone: TriangleAlert, cor: colors.warn, corLinha: colors.warnLine },
  sucesso: { Icone: CheckCircle2, cor: colors.safe, corLinha: colors.safeLine },
  info: { Icone: Bell, cor: colors.accent, corLinha: colors.line },
};

let proximoId = 1;

/**
 * Sprint 17 (ADR 0020) — feedback de erro para ações pontuais (ex.: "abrir portão"
 * falhou) — diferente do ErrorBoundary (erro de renderização) e do estado `error`
 * de cada tela (erro ao carregar dados). Sempre com "Tentar novamente" quando a ação
 * permitir, nunca com texto técnico (a mensagem já chega amigável via
 * `mapErrorToUserMessage`, ver `api/client.ts`).
 *
 * Sprint 18 (ADR 0022) — generalizado para qualquer tipo de toast discreto (não só
 * erro), usado pelo `RealtimeToastBridge` (Fase 3) para eventos em tempo real que
 * acontecem fora da tela em foco. Fila com no máximo 10 mensagens pendentes (Regra
 * 4) — um toast por vez, nunca empilhados visualmente.
 */
export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toast, setToast] = useState<ToastInterno | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const filaRef = useRef<ToastInterno[]>([]);
  const avancarFilaRef = useRef<() => void>(() => {});

  const exibirProximo = useCallback((item: ToastInterno) => {
    setToast(item);
    timeoutRef.current = setTimeout(() => avancarFilaRef.current(), DURACAO_MS);
  }, []);

  const avancarFila = useCallback(() => {
    const proximo = filaRef.current.shift();
    if (proximo) {
      exibirProximo(proximo);
    } else {
      setToast(null);
    }
  }, [exibirProximo]);

  useEffect(() => {
    avancarFilaRef.current = avancarFila;
  }, [avancarFila]);

  const mostrarToast = useCallback(
    (opcoes: ToastOptions) => {
      const item: ToastInterno = { ...opcoes, id: proximoId++ };
      setToast((atual) => {
        if (!atual) {
          exibirProximo(item);
          return atual;
        }
        filaRef.current.push(item);
        if (filaRef.current.length > FILA_MAXIMA) {
          filaRef.current.shift();
        }
        return atual;
      });
    },
    [exibirProximo],
  );

  const mostrarErro = useCallback((opcoes: Omit<ToastOptions, 'tipo'>) => mostrarToast({ ...opcoes, tipo: 'erro' }), [mostrarToast]);

  const fechar = () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    avancarFila();
  };

  const visual = VISUAL_POR_TIPO[toast?.tipo ?? 'erro'];

  return (
    <ToastContext.Provider value={{ mostrarErro, mostrarToast }}>
      {children}
      {toast ? (
        <Animated.View
          key={toast.id}
          entering={FadeInDown.duration(250)}
          exiting={FadeOutDown.duration(250)}
          style={[styles.container, { borderColor: visual.corLinha }]}
        >
          <View style={styles.iconWrap}>
            <visual.Icone size={18} color={visual.cor} />
          </View>
          <View style={styles.textWrap}>
            <Text style={styles.titulo}>{toast.titulo}</Text>
            <Text style={styles.mensagem}>{toast.mensagem}</Text>
            {toast.onRetry ? (
              <Pressable
                onPress={() => {
                  const acao = toast.onRetry;
                  fechar();
                  acao?.();
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
  },
  iconWrap: { marginTop: 2 },
  textWrap: { flex: 1 },
  titulo: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.bold },
  mensagem: { color: colors.sub, fontSize: fontSize.tiny, marginTop: 2 },
  retryBtn: { marginTop: spacing.sm, alignSelf: 'flex-start' },
  retryLabel: { color: colors.accent, fontSize: fontSize.secondary, fontWeight: fontWeight.bold },
});
