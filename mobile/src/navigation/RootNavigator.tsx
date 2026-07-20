import React from 'react';
import { DarkTheme, NavigationContainer, type Theme } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useAuth } from '../auth/AuthContext';
import { SplashScreen } from '../screens/SplashScreen';
import { LoginScreen } from '../screens/LoginScreen';
import { CadastroScreen } from '../screens/CadastroScreen';
import { SelecionarPropriedadeScreen } from '../screens/SelecionarPropriedadeScreen';
import { DashboardScreen } from '../screens/dashboard/DashboardScreen';
import { EventosScreen } from '../screens/eventos/EventosScreen';
import { colors } from '../theme/theme';
import type { RootStackParamList } from './types';

const Stack = createNativeStackNavigator<RootStackParamList>();

const navigationTheme: Theme = {
  ...DarkTheme,
  colors: {
    ...DarkTheme.colors,
    background: colors.bg,
    card: colors.bg,
    text: colors.text,
    border: colors.line,
    primary: colors.safe,
  },
};

/**
 * Navegação orientada pelo estado de auth: sem token -> Login/Cadastro; com token
 * mas sem propriedade selecionada -> SelecionarPropriedade; com as duas coisas ->
 * Dashboard. Splash não é uma rota do Stack — é o que se mostra enquanto o
 * AuthProvider ainda está resolvendo a sessão salva.
 */
export function RootNavigator() {
  const { isLoading, user, selectedProperty } = useAuth();

  if (isLoading) {
    return <SplashScreen />;
  }

  return (
    <NavigationContainer theme={navigationTheme}>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {!user ? (
          <>
            <Stack.Screen name="Login" component={LoginScreen} />
            <Stack.Screen name="Cadastro" component={CadastroScreen} />
          </>
        ) : !selectedProperty ? (
          <Stack.Screen name="SelecionarPropriedade" component={SelecionarPropriedadeScreen} />
        ) : (
          <>
            <Stack.Screen name="Dashboard" component={DashboardScreen} />
            <Stack.Screen name="Eventos" component={EventosScreen} />
          </>
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}
