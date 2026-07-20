import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronRight, History } from 'lucide-react-native';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

/** Atalho para a Central de Eventos — ponto de entrada a partir do Dashboard. */
export function AtalhoEventos() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();

  return (
    <Pressable
      style={styles.card}
      onPress={() => navigation.navigate('Eventos')}
      accessibilityRole="button"
      accessibilityLabel="Ver histórico de eventos"
    >
      <View style={styles.iconWrap}>
        <History size={iconSize.md} color={colors.accent} />
      </View>
      <Text style={styles.label}>Ver histórico de eventos</Text>
      <ChevronRight size={18} color={colors.mute} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  iconWrap: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  label: { flex: 1, color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
});