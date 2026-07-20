import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { LogOut } from 'lucide-react-native';
import { rotuloTipoPropriedade, type TipoPropriedade } from '../../components/TipoPropriedadeSelector';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

interface Props {
  primeiroNome: string;
  nomePropriedade: string;
  tipoPropriedade: TipoPropriedade;
  onLogout: () => void;
}

export function HeaderDashboard({ primeiroNome, nomePropriedade, tipoPropriedade, onLogout }: Props) {
  return (
    <View style={styles.header}>
      <View>
        <Text style={styles.greeting}>Olá, {primeiroNome}</Text>
        <View style={styles.titleRow}>
          <Text style={styles.propertyName}>{nomePropriedade}</Text>
          <View style={styles.badge}>
            <Text style={styles.badgeLabel}>{rotuloTipoPropriedade(tipoPropriedade)}</Text>
          </View>
        </View>
      </View>
      <Pressable onPress={onLogout} style={styles.iconBtn}>
        <LogOut size={18} color={colors.sub} />
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.lg,
  },
  greeting: { color: colors.sub, fontSize: fontSize.secondary },
  titleRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, marginTop: 2 },
  propertyName: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold },
  badge: {
    paddingHorizontal: spacing.xs + 2,
    paddingVertical: 2,
    borderRadius: radius.sm,
    backgroundColor: colors.surface2,
  },
  badgeLabel: { color: colors.sub, fontSize: fontSize.label, fontWeight: fontWeight.medium },
  iconBtn: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
