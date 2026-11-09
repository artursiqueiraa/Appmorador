import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { UserPlus } from 'lucide-react-native';
import { api, ApiError } from '../api/client';
import type { PerfilPropriedade } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { PrimaryButton } from '../components/PrimaryButton';
import { PAPEIS } from '../pessoas/papeis';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import { conviteStore, useConvitePendente } from './conviteStore';

interface ConvitePublico {
  valido: boolean;
  motivo?: string | null;
  nomePropriedade?: string | null;
  papel?: PerfilPropriedade | null;
  email?: string | null;
  convidadoPor?: string | null;
}

/**
 * Sprint 25 (ADR 0041) — cartão por cima do app quando a pessoa abriu um link de convite. Sem
 * login, pede para entrar ou criar a conta (o cartão volta depois); logada, aceita na hora e cai na
 * lista de propriedades, já com a nova.
 */
export function CartaoConvite() {
  const codigo = useConvitePendente();
  const { user, trocarPropriedade } = useAuth();
  const [convite, setConvite] = useState<ConvitePublico | null>(null);
  const [aceitando, setAceitando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [minimizado, setMinimizado] = useState(false);

  useEffect(() => {
    setConvite(null);
    setErro(null);
    if (!codigo) {
      return;
    }
    api
      .get<ConvitePublico>(`/api/convites/codigo/${codigo}`)
      .then(setConvite)
      .catch(() => setConvite({ valido: false, motivo: 'Não foi possível abrir o convite agora.' }));
  }, [codigo]);

  useEffect(() => {
    // Ao entrar, o cartão volta a aparecer para aceitar.
    if (user) {
      setMinimizado(false);
    }
  }, [user]);

  if (!codigo || !convite || minimizado) {
    return null;
  }

  const aceitar = async () => {
    setAceitando(true);
    setErro(null);
    try {
      await api.post(`/api/convites/codigo/${codigo}/aceitar`);
      conviteStore.definir(null);
      trocarPropriedade();
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível aceitar o convite.');
    } finally {
      setAceitando(false);
    }
  };

  const papel = convite.papel ? PAPEIS[convite.papel] : null;

  return (
    <View style={styles.fundo} pointerEvents="box-none">
      <View style={styles.cartao} accessibilityViewIsModal>
        <View style={styles.icone}>
          <UserPlus size={22} color={colors.marca} />
        </View>
        {convite.valido ? (
          <>
            <Text style={styles.titulo}>Você foi convidado</Text>
            <Text style={styles.texto}>
              <Text style={styles.destaque}>{convite.convidadoPor ?? 'Alguém'}</Text> convidou você para acessar{' '}
              <Text style={styles.destaque}>{convite.nomePropriedade}</Text>
              {papel ? ` como ${papel.rotulo}.` : '.'}
            </Text>
            {papel ? <Text style={styles.detalhe}>{papel.descricao}</Text> : null}
            {erro ? <Text style={styles.erro}>{erro}</Text> : null}
            {user ? (
              <PrimaryButton label="Aceitar convite" onPress={aceitar} loading={aceitando} />
            ) : (
              <Text style={styles.detalhe}>
                Entre com sua conta ou crie uma{convite.email ? ` com ${convite.email}` : ''} para aceitar. Este convite fica guardado.
              </Text>
            )}
            <PrimaryButton label={user ? 'Agora não' : 'Entrar ou criar conta'} variant="secondary" onPress={() => setMinimizado(true)} />
          </>
        ) : (
          <>
            <Text style={styles.titulo}>Convite indisponível</Text>
            <Text style={styles.texto}>{convite.motivo ?? 'Este convite não vale mais.'}</Text>
            <PrimaryButton label="Fechar" variant="secondary" onPress={() => conviteStore.definir(null)} />
          </>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  fundo: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(5,8,12,0.78)',
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.xl,
    zIndex: 90,
    elevation: 90,
  },
  cartao: {
    width: '100%',
    maxWidth: 440,
    gap: spacing.md,
    padding: spacing.xl,
    borderRadius: radius.xl,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  icone: { width: 48, height: 48, borderRadius: radius.lg, backgroundColor: colors.marcaDim, alignItems: 'center', justifyContent: 'center' },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  texto: { color: colors.text, fontSize: fontSize.body, lineHeight: 21 },
  destaque: { fontWeight: fontWeight.bold },
  detalhe: { color: colors.sub, fontSize: fontSize.secondary, lineHeight: 19 },
  erro: { color: colors.danger, fontSize: fontSize.secondary },
});
