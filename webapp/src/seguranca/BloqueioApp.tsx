import React, { useCallback, useEffect, useRef, useState } from 'react';
import { AppState, Platform, StyleSheet, Text, View } from 'react-native';
import { Fingerprint, LogOut } from 'lucide-react-native';
import { useAuth } from '../auth/AuthContext';
import { PrimaryButton } from '../components/PrimaryButton';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import { biometria } from './biometria';

/** Depois de quanto tempo em segundo plano o app pede a digital/rosto de novo. */
export const TEMPO_PARA_BLOQUEAR_MS = 5 * 60 * 1000;

/**
 * Sprint 27 (ADR 0043) — "Bloquear o app" (Ajustes → Segurança, desligado por padrão): pede a
 * digital/rosto ao abrir o app e ao voltar depois de 5 minutos em segundo plano. Só no celular.
 * Fica abaixo da tela de alerta crítico: um disparo continua visível mesmo com o app bloqueado.
 */
export function BloqueioApp() {
  const { user, logout } = useAuth();
  const [bloqueado, setBloqueado] = useState(false);
  const foiParaSegundoPlanoEm = useRef<number | null>(null);
  const autenticando = useRef(false);

  const desbloquear = useCallback(async () => {
    if (autenticando.current) {
      return;
    }
    autenticando.current = true;
    try {
      if (await biometria.autenticar('Desbloquear o Akteck Seg')) {
        setBloqueado(false);
      }
    } finally {
      autenticando.current = false;
    }
  }, []);

  // Ao entrar (app aberto do zero com sessão), bloqueia se a opção estiver ligada.
  useEffect(() => {
    if (Platform.OS === 'web' || !user) {
      setBloqueado(false);
      return;
    }
    let ativo = true;
    biometria.bloqueioAtivo().then((ligado) => {
      if (ativo && ligado) {
        setBloqueado(true);
        desbloquear();
      }
    });
    return () => {
      ativo = false;
    };
    // Só na abertura do app / troca de usuário.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.id]);

  useEffect(() => {
    if (Platform.OS === 'web' || !user) {
      return;
    }
    const assinatura = AppState.addEventListener('change', async (estado) => {
      if (estado === 'background') {
        foiParaSegundoPlanoEm.current = Date.now();
        return;
      }
      if (estado !== 'active' || foiParaSegundoPlanoEm.current === null) {
        return;
      }
      const ausente = Date.now() - foiParaSegundoPlanoEm.current;
      foiParaSegundoPlanoEm.current = null;
      if (ausente >= TEMPO_PARA_BLOQUEAR_MS && (await biometria.bloqueioAtivo())) {
        setBloqueado(true);
        desbloquear();
      }
    });
    return () => assinatura.remove();
  }, [user, desbloquear]);

  if (!bloqueado || !user) {
    return null;
  }

  return (
    <View style={styles.tela} accessibilityViewIsModal>
      <View style={styles.icone}>
        <Fingerprint size={40} color={colors.marca} />
      </View>
      <Text style={styles.titulo}>App bloqueado</Text>
      <Text style={styles.texto}>Use a digital, o rosto ou a senha do celular para continuar.</Text>
      <View style={styles.botoes}>
        <PrimaryButton label="Desbloquear" icone={Fingerprint} onPress={desbloquear} />
        <PrimaryButton label="Sair da conta" icone={LogOut} variant="secondary" onPress={() => logout()} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  tela: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: colors.bg,
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.xl,
    gap: spacing.md,
  },
  icone: {
    width: 80,
    height: 80,
    borderRadius: radius.xl,
    backgroundColor: colors.marcaDim,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  texto: { color: colors.sub, fontSize: fontSize.body, textAlign: 'center', maxWidth: 320 },
  botoes: { width: '100%', maxWidth: 320, gap: spacing.sm, marginTop: spacing.md },
});
