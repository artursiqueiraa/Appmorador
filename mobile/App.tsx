import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { ErrorBoundary } from './src/components/ErrorBoundary';
import { ToastProvider } from './src/components/Toast';
import { AuthProvider } from './src/auth/AuthContext';
import { RealtimeProvider } from './src/realtime/RealtimeContext';
import { RootNavigator } from './src/navigation/RootNavigator';

export default function App() {
  return (
    <ErrorBoundary>
      <ToastProvider>
        <AuthProvider>
          <RealtimeProvider>
            <StatusBar style="light" />
            <RootNavigator />
          </RealtimeProvider>
        </AuthProvider>
      </ToastProvider>
    </ErrorBoundary>
  );
}
