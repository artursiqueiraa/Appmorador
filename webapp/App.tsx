import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { ErrorBoundary } from './src/components/ErrorBoundary';
import { ToastProvider } from './src/components/Toast';
import { DialogoAlertaHost } from './src/components/DialogoAlerta';
import { ConfirmacaoComandoHost } from './src/seguranca/ConfirmacaoComandoHost';
import { AuthProvider } from './src/auth/AuthContext';
import { RealtimeProvider } from './src/realtime/RealtimeContext';
import { PushNotificationProvider } from './src/notifications/PushNotificationProvider';
import { RootNavigator } from './src/navigation/RootNavigator';
import { aplicarEstilosGlobaisWeb } from './src/layout/estilosGlobaisWeb';

aplicarEstilosGlobaisWeb();

/**
 * Sprint 18.1 (hotfix) — sem `SafeAreaProvider`, `useSafeAreaInsets`/`SafeAreaView`
 * não têm de onde ler os insets reais do dispositivo; `BottomNavigation.tsx` era o
 * único consumidor até aqui (funcionava por sorte/fallback), mas qualquer tela fora
 * da Bottom Tab Bar (ex.: `SelecionarPropriedadeScreen`) nunca tinha proteção
 * nenhuma contra a barra de navegação do Android — texto/botão no rodapé ficava
 * cortado. Precisa envolver toda a árvore, o mais externo possível.
 */
export default function App() {
  return (
    <SafeAreaProvider>
      <ErrorBoundary>
        <ToastProvider>
          <AuthProvider>
            <PushNotificationProvider>
              <RealtimeProvider>
                <StatusBar style="light" />
                <RootNavigator />
                <DialogoAlertaHost />
                <ConfirmacaoComandoHost />
              </RealtimeProvider>
            </PushNotificationProvider>
          </AuthProvider>
        </ToastProvider>
      </ErrorBoundary>
    </SafeAreaProvider>
  );
}
