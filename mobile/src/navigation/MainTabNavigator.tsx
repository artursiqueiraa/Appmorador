import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { HomeScreen } from '../screens/home/HomeScreen';
import { CamerasScreen } from '../screens/cameras/CamerasScreen';
import { AccessScreen } from '../screens/acessos/AccessScreen';
import { SettingsScreen } from '../screens/ajustes/SettingsScreen';
import { BottomNavigation } from '../components/BottomNavigation';
import type { MainTabParamList } from './types';

const Tab = createBottomTabNavigator<MainTabParamList>();

/** Sprint 16 (ADR 0019, UX001) — as 4 abas fixas, sempre visíveis (Navegação Previsível). */
export function MainTabNavigator() {
  return (
    <Tab.Navigator screenOptions={{ headerShown: false }} tabBar={(props) => <BottomNavigation {...props} />}>
      <Tab.Screen name="Inicio" component={HomeScreen} />
      <Tab.Screen name="Cameras" component={CamerasScreen} />
      <Tab.Screen name="Acessos" component={AccessScreen} />
      <Tab.Screen name="Ajustes" component={SettingsScreen} />
    </Tab.Navigator>
  );
}
