import React, { useState } from 'react';
import { ActivityIndicator, Alert, Pressable, ScrollView, StyleSheet, Switch, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Bell, ChevronRight, FileText, Home, LogOut, User, Wrench } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import type { RootStackParamList } from '../../navigation/types';
import { PropertyCard } from '../../components/PropertyCard';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

/**
 * Sprint 16 (ADR 0019, UX001) — aba "Ajustes". "Minha Propriedade" é o caminho
 * permanente para configuração — nunca depende de logout/login (corrige o bug do
 * onboarding sumir, ver ADR 0019).
 *
 * Sprint 17 (ADR 0020) — reorganizado em Propriedade/Conta/Legal (achado #6 da
 * auditoria: reduzir a rolagem até a ação mais usada). "Modo técnico" é o toggle de
 * `perfil` (ver `auth/profilePreference.ts`) — discreto, dentro de Conta, não uma
 * fronteira de segurança real.
 */
export function SettingsScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { user, selectedProperty, perfil, setPerfil, logout } = useAuth();
  const [saindo, setSaindo] = useState(false);

  // Sprint 18.1 (hotfix) — sem feedback visual, um logout lento (rede ruim) parecia
  // "não fazer nada" para o morador, que às vezes tocava várias vezes sem efeito
  // (o botão de confirmação do Alert nativo já evita múltiplos disparos simultâneos,
  // mas nada indicava que o app estava de fato processando o pedido).
  const confirmarSaida = () => {
    Alert.alert('Sair da conta?', 'Você pode entrar de novo quando quiser.', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Sair',
        style: 'destructive',
        onPress: async () => {
          setSaindo(true);
          try {
            await logout();
          } finally {
            setSaindo(false);
          }
        },
      },
    ]);
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.titulo}>Ajustes</Text>

      <View style={styles.perfil}>
        <View style={styles.avatar}>
          <User size={iconSize.lg} color={colors.accent} />
        </View>
        <View style={styles.perfilTexto}>
          <Text style={styles.nome}>{user?.nome}</Text>
          <Text style={styles.email}>{user?.email}</Text>
        </View>
      </View>

      {selectedProperty ? (
        <View style={styles.secao}>
          <Text style={styles.secaoTitulo}>Propriedade</Text>
          <PropertyCard
            nome={selectedProperty.nome}
            tipo={selectedProperty.tipo}
            endereco={selectedProperty.endereco}
            onPress={() => navigation.navigate('MinhaPropriedade')}
          />
        </View>
      ) : null}

      <View style={styles.secao}>
        <Text style={styles.secaoTitulo}>Conta</Text>
        <MenuLinha icon={Bell} label="Notificações" onPress={() => navigation.navigate('Notificacoes')} />
        <View style={styles.toggleLinha}>
          <View style={styles.itemIconWrap}>
            <Wrench size={iconSize.sm} color={colors.sub} />
          </View>
          <View style={styles.toggleTextoWrap}>
            <Text style={styles.itemTitulo}>Modo técnico</Text>
            <Text style={styles.toggleDescricao}>Mostra telas de configuração avançada de equipamentos.</Text>
          </View>
          <Switch
            value={perfil === 'tecnico'}
            onValueChange={(ativo) => setPerfil(ativo ? 'tecnico' : 'morador')}
            trackColor={{ false: colors.surface2, true: colors.safeDim }}
            thumbColor={perfil === 'tecnico' ? colors.safe : colors.mute}
          />
        </View>
      </View>

      <View style={styles.secao}>
        <Text style={styles.secaoTitulo}>Legal</Text>
        <MenuLinha icon={FileText} label="Termos e Privacidade" onPress={() => Alert.alert('Termos e Privacidade', 'appmorador.com.br/termos')} />
      </View>

      <View style={styles.secao}>
        <Pressable style={[styles.sair, saindo && styles.sairDesabilitado]} onPress={confirmarSaida} disabled={saindo}>
          {saindo ? (
            <ActivityIndicator color={colors.danger} size="small" />
          ) : (
            <>
              <LogOut size={iconSize.sm} color={colors.danger} />
              <Text style={styles.sairTexto}>Sair</Text>
            </>
          )}
        </Pressable>
      </View>
    </ScrollView>
  );
}

function MenuLinha({ icon: Icon, label, onPress }: { icon: typeof Home; label: string; onPress: () => void }) {
  return (
    <Pressable style={styles.itemLinha} onPress={onPress}>
      <View style={styles.itemIconWrap}>
        <Icon size={iconSize.sm} color={colors.sub} />
      </View>
      <Text style={styles.itemTitulo}>{label}</Text>
      <View style={{ flex: 1 }} />
      <ChevronRight size={16} color={colors.mute} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black, marginBottom: spacing.lg },
  perfil: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.xl },
  avatar: {
    width: 56,
    height: 56,
    borderRadius: 999,
    backgroundColor: colors.surface2,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  perfilTexto: { flex: 1, minWidth: 0 },
  nome: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  email: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  secao: { marginBottom: spacing.lg, gap: spacing.sm },
  secaoTitulo: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, marginBottom: spacing.xs },
  toggleLinha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  toggleTextoWrap: { flex: 1, minWidth: 0 },
  toggleDescricao: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  itemLinha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  itemIconWrap: {
    width: 34,
    height: 34,
    borderRadius: radius.sm,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  itemTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  sair: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.dangerDim,
    borderWidth: 1,
    borderColor: colors.dangerLine,
  },
  sairTexto: { color: colors.danger, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  sairDesabilitado: { opacity: 0.6 },
});
