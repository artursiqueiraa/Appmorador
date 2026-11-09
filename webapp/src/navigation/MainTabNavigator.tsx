import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { HomeScreen } from '../screens/home/HomeScreen';
import { CamerasScreen } from '../screens/cameras/CamerasScreen';
import { AccessScreen } from '../screens/acessos/AccessScreen';
import { AlarmeScreen } from '../screens/alarme/AlarmeScreen';
import { BottomNavigation } from '../components/BottomNavigation';
import { ColunaConteudo } from '../layout/ColunaConteudo';
import { useLayoutDesktop } from '../layout/useLayoutDesktop';
import { layout } from '../theme/theme';
import type { MainTabParamList } from './types';

const Tab = createBottomTabNavigator<MainTabParamList>();

/**
 * Sprint 16 (ADR 0019, UX001) — as 4 abas fixas, sempre visíveis (Navegação Previsível).
 * Web App — em telas largas as mesmas 4 abas viram barra lateral à esquerda (nunca
 * somem), e o conteúdo de cada aba fica numa coluna centralizada.
 */
export function MainTabNavigator() {
  const desktop = useLayoutDesktop();

  return (
    <Tab.Navigator
      screenOptions={{ headerShown: false, tabBarPosition: desktop ? 'left' : 'bottom' }}
      screenLayout={({ route, children }) => (
        <ColunaConteudo larguraMaxima={route.name === 'Alarme' ? layout.contentMaxWidth : layout.wideMaxWidth} recuoInferior={false}>{children}</ColunaConteudo>
      )}
      tabBar={(props) => <BottomNavigation {...props} lateral={desktop} />}
    >
      <Tab.Screen name="Inicio" component={HomeScreen} />
      <Tab.Screen name="Cameras" component={CamerasScreen} />
      <Tab.Screen name="Alarme" component={AlarmeScreen} />
      <Tab.Screen name="Acessos" component={AccessScreen} />
    </Tab.Navigator>
  );
}
