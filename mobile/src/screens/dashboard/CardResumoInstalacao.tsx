import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Cpu, HardDrive, Radio, Video } from 'lucide-react-native';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

interface Props {
  quantidadeCentrais: number;
  quantidadeGravadores: number;
  quantidadeCameras: number;
  quantidadeSensores: number;
}

/** "Resumo da instalação" — comunica o que está protegendo a propriedade, em linguagem simples. */
export function CardResumoInstalacao({ quantidadeCentrais, quantidadeGravadores, quantidadeCameras, quantidadeSensores }: Props) {
  const itens = [
    { icone: Cpu, valor: quantidadeCentrais, rotulo: 'Centrais' },
    { icone: HardDrive, valor: quantidadeGravadores, rotulo: 'Gravadores' },
    { icone: Video, valor: quantidadeCameras, rotulo: 'Câmeras' },
    { icone: Radio, valor: quantidadeSensores, rotulo: 'Sensores' },
  ];

  return (
    <View style={styles.card}>
      <Text style={styles.titulo}>Resumo da instalação</Text>
      <View style={styles.itemsRow}>
        {itens.map((item) => (
          <View key={item.rotulo} style={styles.item}>
            <item.icone size={iconSize.md} color={colors.accent} />
            <Text style={styles.itemValor}>{item.valor}</Text>
            <Text style={styles.itemRotulo}>{item.rotulo}</Text>
          </View>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    padding: spacing.lg,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.md,
  },
  titulo: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, marginBottom: spacing.md },
  itemsRow: { flexDirection: 'row', justifyContent: 'space-between' },
  item: { alignItems: 'center', gap: 4, flex: 1 },
  itemValor: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  itemRotulo: { color: colors.mute, fontSize: fontSize.label },
});
