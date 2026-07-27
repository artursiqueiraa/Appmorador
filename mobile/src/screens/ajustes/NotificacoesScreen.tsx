import React, { useCallback, useState } from 'react';
import { ActivityIndicator, Linking, Pressable, ScrollView, StyleSheet, Switch, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Bell, BellOff, ChevronLeft, Home, Package, ShieldAlert } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import type { RootStackParamList } from '../../navigation/types';
import { obterPreferenciasLocais, salvarPreferenciasLocais, type PreferenciasNotificacaoLocais } from '../../notifications/pushDeviceStorage';
import { atualizarPreferenciasAsync, obterStatusPermissaoAsync, solicitarPermissaoERegistrarAsync, type StatusPermissaoPush } from '../../notifications/pushService';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

/**
 * Sprint 19 (ADR 0023, Fase 7.2) — nunca usa termos técnicos ("canal", "FCM",
 * "token"): os 3 grupos aparecem como "Alarmes e alertas" / "Atividades em casa"
 * / "Mudanças de status", os mesmos nomes usados nos canais Android (Fase 9),
 * para o morador nunca ver dois vocabulários diferentes para a mesma coisa.
 */
export function NotificacoesScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const [status, setStatus] = useState<StatusPermissaoPush | null>(null);
  const [preferencias, setPreferencias] = useState<PreferenciasNotificacaoLocais | null>(null);
  const [ativando, setAtivando] = useState(false);

  const carregar = useCallback(async () => {
    const [statusAtual, prefsAtuais] = await Promise.all([obterStatusPermissaoAsync(), obterPreferenciasLocais()]);
    setStatus(statusAtual);
    setPreferencias(prefsAtuais);
  }, []);

  // Sprint 19 — recarrega toda vez que a tela ganha foco: o morador pode ter
  // ativado a permissão pelas Configurações do celular (via "Abrir configurações"
  // abaixo) e voltado para o app, sem passar por nenhum evento que este
  // componente já escute.
  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar]),
  );

  const ativarNotificacoes = async () => {
    setAtivando(true);
    try {
      const novoStatus = await solicitarPermissaoERegistrarAsync(selectedProperty?.id ?? null);
      setStatus(novoStatus);
    } finally {
      setAtivando(false);
    }
  };

  const alterarPreferencia = async (campo: keyof PreferenciasNotificacaoLocais, valor: boolean) => {
    if (!preferencias) {
      return;
    }
    const novasPreferencias = { ...preferencias, [campo]: valor };
    setPreferencias(novasPreferencias);
    await salvarPreferenciasLocais(novasPreferencias);
    await atualizarPreferenciasAsync(novasPreferencias).catch(() => {
      // best-effort — a preferência já foi salva localmente e será reenviada na próxima alteração.
    });
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <Text style={styles.titulo}>Notificações</Text>
      </View>

      {status === null ? (
        <ActivityIndicator color={colors.accent} style={{ marginTop: spacing.xl }} />
      ) : status !== 'concedida' ? (
        <View style={styles.avisoDesativado}>
          <BellOff size={iconSize.lg} color={colors.warn} />
          <Text style={styles.avisoTitulo}>Notificações desativadas</Text>
          <Text style={styles.avisoTexto}>
            Ative para ser avisado sobre alarmes, visitantes, entregas e outras atividades mesmo com o app fechado.
          </Text>
          <Pressable
            style={[styles.botaoAtivar, ativando && styles.botaoDesabilitado]}
            onPress={status === 'negada' ? () => Linking.openSettings() : ativarNotificacoes}
            disabled={ativando}
          >
            {ativando ? (
              <ActivityIndicator color={colors.bg} size="small" />
            ) : (
              <>
                <Bell size={iconSize.sm} color={colors.bg} />
                <Text style={styles.botaoAtivarTexto}>
                  {status === 'negada' ? 'Abrir configurações do celular' : 'Ativar notificações'}
                </Text>
              </>
            )}
          </Pressable>
        </View>
      ) : (
        <View style={styles.secao}>
          <ToggleLinha
            icon={ShieldAlert}
            titulo="Alarmes e alertas"
            descricao="Alarme disparado e dispositivos que pararam de responder."
            valor={preferencias?.notificarAlertas ?? true}
            onValueChange={(valor) => alterarPreferencia('notificarAlertas', valor)}
          />
          <ToggleLinha
            icon={Package}
            titulo="Atividades em casa"
            descricao="Comandos acionados, visitantes autorizados e entregas registradas."
            valor={preferencias?.notificarAtividades ?? true}
            onValueChange={(valor) => alterarPreferencia('notificarAtividades', valor)}
          />
          <ToggleLinha
            icon={Home}
            titulo="Mudanças de status"
            descricao="Quando o sistema é armado ou desarmado."
            valor={preferencias?.notificarGeral ?? true}
            onValueChange={(valor) => alterarPreferencia('notificarGeral', valor)}
          />
        </View>
      )}
    </ScrollView>
  );
}

function ToggleLinha({
  icon: Icon,
  titulo,
  descricao,
  valor,
  onValueChange,
}: {
  icon: typeof Home;
  titulo: string;
  descricao: string;
  valor: boolean;
  onValueChange: (valor: boolean) => void;
}) {
  return (
    <View style={styles.toggleLinha}>
      <View style={styles.itemIconWrap}>
        <Icon size={iconSize.sm} color={colors.sub} />
      </View>
      <View style={styles.toggleTextoWrap}>
        <Text style={styles.itemTitulo}>{titulo}</Text>
        <Text style={styles.toggleDescricao}>{descricao}</Text>
      </View>
      <Switch
        value={valor}
        onValueChange={onValueChange}
        trackColor={{ false: colors.surface2, true: colors.safeDim }}
        thumbColor={valor ? colors.safe : colors.mute}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.xl },
  iconBtn: {
    width: 36,
    height: 36,
    borderRadius: radius.sm,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  secao: { gap: spacing.sm },
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
  itemIconWrap: {
    width: 34,
    height: 34,
    borderRadius: radius.sm,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  itemTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  avisoDesativado: {
    alignItems: 'center',
    gap: spacing.sm,
    padding: spacing.xl,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  avisoTitulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold, marginTop: spacing.xs },
  avisoTexto: { color: colors.mute, fontSize: fontSize.secondary, textAlign: 'center' },
  botaoAtivar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    marginTop: spacing.sm,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.xl,
    borderRadius: radius.lg,
    backgroundColor: colors.accent,
  },
  botaoAtivarTexto: { color: colors.bg, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  botaoDesabilitado: { opacity: 0.6 },
});
