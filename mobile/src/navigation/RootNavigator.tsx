import React from 'react';
import { DarkTheme, NavigationContainer, type Theme } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useAuth } from '../auth/AuthContext';
import { SplashScreen } from '../screens/SplashScreen';
import { LoginScreen } from '../screens/LoginScreen';
import { CadastroScreen } from '../screens/CadastroScreen';
import { SelecionarPropriedadeScreen } from '../screens/SelecionarPropriedadeScreen';
import { OnboardingWizardScreen } from '../onboarding/OnboardingWizard/OnboardingWizardScreen';
import { MainTabNavigator } from './MainTabNavigator';
import { EventosScreen } from '../screens/eventos/EventosScreen';
import { DetalheCameraScreen } from '../screens/cameras/DetalheCameraScreen';
import { MinhaPropriedadeScreen } from '../screens/ajustes/MinhaPropriedadeScreen';
import { NotificacoesScreen } from '../screens/ajustes/NotificacoesScreen';
import { UnidadesScreen } from '../screens/unidades/UnidadesScreen';
import { MoradoresScreen } from '../screens/moradores/MoradoresScreen';
import { CredenciaisScreen } from '../screens/credenciais/CredenciaisScreen';
import { PermissoesScreen } from '../screens/permissoes/PermissoesScreen';
import { PontosAcessoScreen } from '../screens/pontosAcesso/PontosAcessoScreen';
import { VisitantesScreen } from '../screens/visitantes/VisitantesScreen';
import { AutorizacoesScreen } from '../screens/autorizacoes/AutorizacoesScreen';
import { VeiculosScreen } from '../screens/veiculos/VeiculosScreen';
import { VagasScreen } from '../screens/vagas/VagasScreen';
import { EntregasScreen } from '../screens/entregas/EntregasScreen';
import { DetalhesEntregaScreen } from '../screens/entregas/DetalhesEntregaScreen';
import { EquipamentosScreen } from '../screens/equipamentos/EquipamentosScreen';
import { DetalhesEquipamentoScreen } from '../screens/equipamentos/DetalhesEquipamentoScreen';
import { CentraisJflScreen } from '../screens/centraisJfl/CentraisJflScreen';
import { DetalhesCentralJflScreen } from '../screens/centraisJfl/DetalhesCentralJflScreen';
import { CentraisIntelbrasScreen } from '../screens/centraisIntelbras/CentraisIntelbrasScreen';
import { DetalhesCentralIntelbrasScreen } from '../screens/centraisIntelbras/DetalhesCentralIntelbrasScreen';
import { CentralOperacionalScreen } from '../screens/operacional/CentralOperacionalScreen';
import { SaudePropriedadeScreen } from '../screens/operacional/SaudePropriedadeScreen';
import { colors } from '../theme/theme';
import { navigationRef } from './navigationRef';
import { definirTelaAtiva } from './telaAtivaStore';
import { RealtimeToastBridge } from '../realtime/RealtimeToastBridge';
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
 * Sprint 16 (ADR 0019, UX001) — navegação orientada pelo estado de auth: sem token
 * -> Login/Cadastro; com token mas sem propriedade selecionada -> SelecionarPropriedade;
 * com as duas coisas -> MainTabs (Início/Câmeras/Acessos/Ajustes). Todas as telas de
 * detalhe (Unidades, Credenciais, Centrais...) agora vivem no MESMO branch de MainTabs
 * — antes viviam só no branch pré-seleção de propriedade e ficavam inalcançáveis
 * depois de entrar no Dashboard (bug de navegação corrigido nesta Sprint). Splash não
 * é uma rota do Stack — é o que se mostra enquanto o AuthProvider ainda está
 * resolvendo a sessão salva.
 */
export function RootNavigator() {
  const { isLoading, user, selectedProperty } = useAuth();

  if (isLoading) {
    return <SplashScreen />;
  }

  return (
    <NavigationContainer
      ref={navigationRef}
      theme={navigationTheme}
      onReady={() => definirTelaAtiva(navigationRef.getCurrentRoute()?.name)}
      onStateChange={() => definirTelaAtiva(navigationRef.getCurrentRoute()?.name)}
    >
      {user && selectedProperty ? <RealtimeToastBridge /> : null}
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {!user ? (
          <>
            <Stack.Screen name="Login" component={LoginScreen} />
            <Stack.Screen name="Cadastro" component={CadastroScreen} />
          </>
        ) : !selectedProperty ? (
          <>
            <Stack.Screen name="SelecionarPropriedade" component={SelecionarPropriedadeScreen} />
            <Stack.Screen name="Onboarding" component={OnboardingWizardScreen} />
          </>
        ) : (
          <>
            <Stack.Screen name="MainTabs" component={MainTabNavigator} />
            <Stack.Screen name="Onboarding" component={OnboardingWizardScreen} />
            <Stack.Screen name="Eventos" component={EventosScreen} />
            <Stack.Screen name="MinhaPropriedade" component={MinhaPropriedadeScreen} />
            <Stack.Screen name="Notificacoes" component={NotificacoesScreen} />
            <Stack.Screen name="Unidades" component={UnidadesScreen} />
            <Stack.Screen name="Moradores" component={MoradoresScreen} />
            <Stack.Screen name="Credenciais" component={CredenciaisScreen} />
            <Stack.Screen name="Permissoes" component={PermissoesScreen} />
            <Stack.Screen name="PontosAcesso" component={PontosAcessoScreen} />
            <Stack.Screen name="Visitantes" component={VisitantesScreen} />
            <Stack.Screen name="Autorizacoes" component={AutorizacoesScreen} />
            <Stack.Screen name="Veiculos" component={VeiculosScreen} />
            <Stack.Screen name="Vagas" component={VagasScreen} />
            <Stack.Screen name="Entregas" component={EntregasScreen} />
            <Stack.Screen name="DetalhesEntrega" component={DetalhesEntregaScreen} />
            <Stack.Screen name="Equipamentos" component={EquipamentosScreen} />
            <Stack.Screen name="DetalhesEquipamento" component={DetalhesEquipamentoScreen} />
            <Stack.Screen name="CentraisJfl" component={CentraisJflScreen} />
            <Stack.Screen name="DetalhesCentralJfl" component={DetalhesCentralJflScreen} />
            <Stack.Screen name="CentraisIntelbras" component={CentraisIntelbrasScreen} />
            <Stack.Screen name="DetalhesCentralIntelbras" component={DetalhesCentralIntelbrasScreen} />
            <Stack.Screen name="CentralOperacional" component={CentralOperacionalScreen} />
            <Stack.Screen name="SaudePropriedade" component={SaudePropriedadeScreen} />
            <Stack.Screen name="DetalheCamera" component={DetalheCameraScreen} />
          </>
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}
