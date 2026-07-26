import React from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';
import { BlurView } from 'expo-blur';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import type { BottomTabBarProps } from '@react-navigation/bottom-tabs';
import { Home, Settings, Users, Video } from 'lucide-react-native';
import { colors, fontSize, fontWeight, spacing } from '../theme/theme';

const ICONES: Record<string, typeof Home> = {
  Inicio: Home,
  Cameras: Video,
  Acessos: Users,
  Ajustes: Settings,
};

const ROTULOS: Record<string, string> = {
  Inicio: 'Início',
  Cameras: 'Câmeras',
  Acessos: 'Acessos',
  Ajustes: 'Ajustes',
};

/**
 * Sprint 16 (ADR 0019, UX001) — navegação inferior fixa, sempre visível, nunca
 * escondida (Princípio de Navegação Previsível). Renderizada como `tabBar` custom
 * do `createBottomTabNavigator` para reaproveitar o gerenciamento de estado/
 * histórico do React Navigation com o visual exato do protótipo.
 */
export function BottomNavigation({ state, navigation }: BottomTabBarProps) {
  const insets = useSafeAreaInsets();

  return (
    <BlurView intensity={40} tint="dark" style={[styles.container, { paddingBottom: Math.max(insets.bottom, spacing.sm) }]}>
      {state.routes.map((route, index) => {
        const ativo = state.index === index;
        const Icone = ICONES[route.name] ?? Home;
        const rotulo = ROTULOS[route.name] ?? route.name;

        return (
          <Pressable
            key={route.key}
            onPress={() => navigation.navigate(route.name)}
            style={styles.item}
            accessibilityRole="tab"
            accessibilityState={{ selected: ativo }}
            accessibilityLabel={rotulo}
          >
            <Icone size={21} color={ativo ? colors.safe : colors.mute} />
            <Text style={[styles.label, ativo && styles.labelAtivo]}>{rotulo}</Text>
          </Pressable>
        );
      })}
    </BlurView>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.lineSoft,
    overflow: 'hidden',
  },
  item: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 4,
    minHeight: 48,
  },
  label: { fontSize: fontSize.label, fontWeight: fontWeight.medium, color: colors.mute },
  labelAtivo: { color: colors.text },
});
