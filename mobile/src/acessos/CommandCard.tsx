import React, { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { Car, DoorOpen, Fence, Lightbulb, Lock, ShieldQuestion } from 'lucide-react-native';
import type { IconePgm } from './pgmLabels';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

const ICONES: Record<IconePgm, typeof DoorOpen> = {
  porta: DoorOpen,
  garagem: Car,
  luz: Lightbulb,
  fechadura: Lock,
  cancela: Fence,
  generico: ShieldQuestion,
};

interface Props {
  icone: IconePgm;
  label: string;
  conectado: boolean;
  descricaoEstado: string;
  /** Retorna se a ação teve sucesso — falha já é comunicada pelo chamador via Toast (com "Tentar novamente"). */
  onAcionar: () => Promise<boolean>;
}

/** Sprint 17 (ADR 0020) — um comando do Painel de Controle: ícone + nome amigável + estado + ação + feedback. */
export function CommandCard({ icone, label, conectado, descricaoEstado, onAcionar }: Props) {
  const [carregando, setCarregando] = useState(false);
  const [feitoAgora, setFeitoAgora] = useState(false);
  const Icone = ICONES[icone];

  const acionar = async () => {
    setCarregando(true);
    setFeitoAgora(false);
    try {
      const sucesso = await onAcionar();
      if (sucesso) {
        setFeitoAgora(true);
        setTimeout(() => setFeitoAgora(false), 3000);
      }
    } finally {
      setCarregando(false);
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.iconWrap}>
        <Icone size={iconSize.md} color={conectado ? colors.accent : colors.mute} />
      </View>
      <View style={styles.textoWrap}>
        <Text style={styles.label}>{label}</Text>
        <Text style={[styles.estado, conectado ? styles.estadoOk : styles.estadoOffline]}>
          {feitoAgora ? 'Feito ✓' : descricaoEstado}
        </Text>
      </View>
      <Pressable
        onPress={acionar}
        disabled={!conectado || carregando}
        accessibilityLabel={conectado ? label : `${label} — dispositivo offline`}
        style={[styles.botao, !conectado && styles.botaoDesabilitado]}
      >
        {carregando ? <ActivityIndicator color={colors.safe} size="small" /> : <Text style={styles.botaoLabel}>{label}</Text>}
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
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
    width: 42,
    height: 42,
    borderRadius: radius.md,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  textoWrap: { flex: 1, minWidth: 0 },
  label: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  estado: { fontSize: fontSize.tiny, marginTop: 2 },
  estadoOk: { color: colors.safe },
  estadoOffline: { color: colors.mute },
  botao: {
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderRadius: radius.md,
    backgroundColor: colors.safeDim,
    borderWidth: 1,
    borderColor: colors.safeLine,
    minWidth: 96,
    alignItems: 'center',
  },
  botaoDesabilitado: { backgroundColor: colors.surface2, borderColor: colors.line, opacity: 0.6 },
  botaoLabel: { color: colors.safe, fontSize: fontSize.meta, fontWeight: fontWeight.bold },
});
