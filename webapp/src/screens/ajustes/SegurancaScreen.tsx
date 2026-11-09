import React, { useCallback, useEffect, useState } from 'react';
import { Platform, Pressable, ScrollView, StyleSheet, Switch, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { CheckCircle2, ChevronLeft, Fingerprint, KeyRound, Lock, XCircle } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import type { RootStackParamList } from '../../navigation/types';
import { biometria } from '../../seguranca/biometria';
import { invalidarSituacaoPin } from '../../seguranca/confirmarComando';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

interface ComandoRegistrado {
  id: string;
  descricao: string;
  autor: string;
  confirmacao: 'Nenhuma' | 'Pin' | 'Biometria';
  sucesso: boolean;
  mensagem?: string | null;
  ocorridoEmUtc: string;
}

const COMO_CONFIRMOU: Record<ComandoRegistrado['confirmacao'], string> = {
  Nenhuma: 'sem confirmação',
  Pin: 'com PIN',
  Biometria: 'com digital/rosto',
};

const soNumeros = (texto: string) => texto.replace(/\D/g, '').slice(0, 6);

/**
 * Sprint 27 (ADR 0043) — PIN que confirma desarmar e abrir, digital/rosto do celular, bloqueio do
 * app e o registro dos últimos comandos da propriedade (dono e gerente).
 */
export function SegurancaScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const nativo = Platform.OS !== 'web';
  const podeVerRegistro = selectedProperty?.perfil === 'Administrador' || selectedProperty?.perfil === 'Gerente';

  const [pinDefinido, setPinDefinido] = useState<boolean | null>(null);
  const [senha, setSenha] = useState('');
  const [novoPin, setNovoPin] = useState('');
  const [repetirPin, setRepetirPin] = useState('');
  const [mensagem, setMensagem] = useState<{ texto: string; erro: boolean } | null>(null);
  const [salvando, setSalvando] = useState(false);

  const [temBiometria, setTemBiometria] = useState(false);
  const [confirmarComBiometria, setConfirmarComBiometria] = useState(false);
  const [bloquearApp, setBloquearApp] = useState(false);
  const [pinParaBiometria, setPinParaBiometria] = useState('');
  const [pedindoPinBiometria, setPedindoPinBiometria] = useState(false);

  const [comandos, setComandos] = useState<ComandoRegistrado[] | null>(null);

  const carregar = useCallback(async () => {
    api
      .get<{ definido: boolean }>('/api/minha-conta/pin')
      .then((r) => setPinDefinido(r.definido))
      .catch(() => setPinDefinido(null));
    setTemBiometria(await biometria.disponivel());
    setConfirmarComBiometria(await biometria.confirmarAtivo());
    setBloquearApp(await biometria.bloqueioAtivo());
    if (selectedProperty && podeVerRegistro) {
      api
        .get<ComandoRegistrado[]>(`/api/properties/${selectedProperty.id}/comandos?limite=20`)
        .then(setComandos)
        .catch(() => setComandos([]));
    }
  }, [selectedProperty, podeVerRegistro]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const salvarPin = async () => {
    setMensagem(null);
    if (novoPin !== repetirPin) {
      setMensagem({ texto: 'Os dois PINs não são iguais.', erro: true });
      return;
    }
    setSalvando(true);
    try {
      await api.put('/api/minha-conta/pin', { senha, pin: novoPin });
      invalidarSituacaoPin();
      if (confirmarComBiometria) {
        await biometria.ativarConfirmacao(novoPin);
      }
      setPinDefinido(true);
      setSenha('');
      setNovoPin('');
      setRepetirPin('');
      setMensagem({ texto: 'PIN salvo. Desarmar e abrir vão pedir este PIN.', erro: false });
    } catch (e) {
      setMensagem({ texto: e instanceof ApiError ? e.message : 'Não foi possível salvar o PIN.', erro: true });
    } finally {
      setSalvando(false);
    }
  };

  const ligarConfirmacaoBiometria = async () => {
    setMensagem(null);
    try {
      await api.post('/api/minha-conta/pin/verificar', { pin: pinParaBiometria });
    } catch (e) {
      setMensagem({ texto: e instanceof ApiError ? e.message : 'Não foi possível conferir o PIN.', erro: true });
      return;
    }
    if (!(await biometria.autenticar('Ligar confirmação com digital/rosto'))) {
      setMensagem({ texto: 'A digital/rosto não foi confirmada.', erro: true });
      return;
    }
    await biometria.ativarConfirmacao(pinParaBiometria);
    setConfirmarComBiometria(true);
    setPedindoPinBiometria(false);
    setPinParaBiometria('');
    setMensagem({ texto: 'Pronto: desarmar e abrir vão pedir a digital/rosto.', erro: false });
  };

  const alternarConfirmacaoBiometria = async (ligar: boolean) => {
    if (ligar) {
      setPedindoPinBiometria(true);
      return;
    }
    await biometria.desativarConfirmacao();
    setConfirmarComBiometria(false);
    setPedindoPinBiometria(false);
  };

  const alternarBloqueio = async (ligar: boolean) => {
    if (ligar && !(await biometria.autenticar('Ligar o bloqueio do app'))) {
      return;
    }
    await biometria.definirBloqueio(ligar);
    setBloquearApp(ligar);
  };

  const pinValido = novoPin.length >= 4 && repetirPin.length >= 4 && senha.length > 0;

  return (
    <ScrollView style={styles.tela} contentContainerStyle={styles.conteudo} keyboardShouldPersistTaps="handled">
      <View style={styles.cabecalho}>
        <Pressable onPress={() => navigation.goBack()} style={styles.voltar} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <Text style={styles.titulo}>Segurança</Text>
      </View>

      {mensagem ? (
        <Text style={[styles.mensagem, mensagem.erro && styles.erro]} accessibilityRole="alert">
          {mensagem.texto}
        </Text>
      ) : null}

      <Text style={styles.secao}>PIN de confirmação</Text>
      <Text style={styles.detalhe}>
        {pinDefinido
          ? 'Seu PIN está ativo: desarmar o alarme e abrir portões pedem o PIN. Para trocar, ou se esqueceu, crie um novo abaixo com a senha da conta.'
          : 'Crie um PIN de 4 a 6 números. Depois disso, desarmar o alarme e abrir portões pedem o PIN, mesmo com o celular desbloqueado.'}
      </Text>
      <TextField label="Senha da conta" value={senha} onChangeText={setSenha} secureTextEntry />
      <TextField
        label={pinDefinido ? 'Novo PIN' : 'PIN (4 a 6 números)'}
        value={novoPin}
        onChangeText={(t) => setNovoPin(soNumeros(t))}
        keyboardType="number-pad"
        secureTextEntry
      />
      <TextField label="Repita o PIN" value={repetirPin} onChangeText={(t) => setRepetirPin(soNumeros(t))} keyboardType="number-pad" secureTextEntry />
      <PrimaryButton label={pinDefinido ? 'Trocar PIN' : 'Criar PIN'} icone={KeyRound} loading={salvando} disabled={!pinValido} onPress={salvarPin} />

      {nativo ? (
        <>
          <Text style={styles.secao}>Digital ou rosto</Text>
          {!temBiometria ? (
            <Text style={styles.detalhe}>Cadastre uma digital ou o rosto nas configurações do celular para usar estas opções.</Text>
          ) : (
            <>
              <Linha
                Icone={Fingerprint}
                titulo="Confirmar com digital/rosto"
                descricao="Em vez de digitar o PIN para desarmar e abrir."
                valor={confirmarComBiometria || pedindoPinBiometria}
                desabilitado={!pinDefinido}
                onValueChange={alternarConfirmacaoBiometria}
              />
              {pedindoPinBiometria && !confirmarComBiometria ? (
                <View style={styles.bloco}>
                  <TextField
                    label="Digite seu PIN para ligar"
                    value={pinParaBiometria}
                    onChangeText={(t) => setPinParaBiometria(soNumeros(t))}
                    keyboardType="number-pad"
                    secureTextEntry
                  />
                  <PrimaryButton label="Ligar" icone={Fingerprint} disabled={pinParaBiometria.length < 4} onPress={ligarConfirmacaoBiometria} />
                </View>
              ) : null}
              <Linha
                Icone={Lock}
                titulo="Bloquear o app"
                descricao="Pede a digital/rosto ao abrir o app e ao voltar depois de 5 minutos."
                valor={bloquearApp}
                onValueChange={alternarBloqueio}
              />
            </>
          )}
        </>
      ) : null}

      {podeVerRegistro ? (
        <>
          <Text style={styles.secao}>Últimos comandos</Text>
          <Text style={styles.detalhe}>Quem armou, desarmou e abriu nesta propriedade.</Text>
          {comandos === null ? null : comandos.length === 0 ? (
            <Text style={styles.detalhe}>Nenhum comando registrado ainda.</Text>
          ) : (
            comandos.map((c) => (
              <View key={c.id} style={styles.comando}>
                {c.sucesso ? <CheckCircle2 size={18} color={colors.safe} /> : <XCircle size={18} color={colors.danger} />}
                <View style={{ flex: 1 }}>
                  <Text style={styles.comandoTitulo}>{c.descricao}</Text>
                  <Text style={styles.comandoMeta}>
                    {c.autor} · {COMO_CONFIRMOU[c.confirmacao]} ·{' '}
                    {new Date(c.ocorridoEmUtc).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
                  </Text>
                  {!c.sucesso && c.mensagem ? <Text style={styles.comandoErro}>{c.mensagem}</Text> : null}
                </View>
              </View>
            ))
          )}
        </>
      ) : null}
    </ScrollView>
  );
}

function Linha({
  Icone,
  titulo,
  descricao,
  valor,
  desabilitado,
  onValueChange,
}: {
  Icone: typeof Lock;
  titulo: string;
  descricao: string;
  valor: boolean;
  desabilitado?: boolean;
  onValueChange: (v: boolean) => void;
}) {
  return (
    <View style={[styles.linha, desabilitado && { opacity: 0.5 }]}>
      <Icone size={20} color={colors.sub} />
      <View style={{ flex: 1 }}>
        <Text style={styles.linhaTitulo}>{titulo}</Text>
        <Text style={styles.detalhe}>{desabilitado ? 'Crie o PIN primeiro.' : descricao}</Text>
      </View>
      <Switch
        value={valor}
        disabled={desabilitado}
        onValueChange={onValueChange}
        accessibilityLabel={titulo}
        trackColor={{ false: colors.surface2, true: colors.marcaDim }}
        thumbColor={valor ? colors.marca : colors.mute}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  tela: { flex: 1, backgroundColor: colors.bg },
  conteudo: { padding: spacing.xl, gap: spacing.sm, paddingBottom: spacing.xxxl },
  cabecalho: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.sm },
  voltar: {
    width: 44,
    height: 44,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  secao: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold, marginTop: spacing.lg },
  detalhe: { color: colors.sub, fontSize: fontSize.secondary },
  mensagem: { color: colors.safe, fontSize: fontSize.secondary },
  erro: { color: colors.danger },
  bloco: { gap: spacing.sm, paddingLeft: spacing.xl },
  linha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.line,
    padding: spacing.md,
  },
  linhaTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  comando: {
    flexDirection: 'row',
    gap: spacing.md,
    alignItems: 'flex-start',
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.line,
    padding: spacing.md,
  },
  comandoTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  comandoMeta: { color: colors.sub, fontSize: fontSize.meta, marginTop: 2 },
  comandoErro: { color: colors.danger, fontSize: fontSize.meta, marginTop: 2 },
});
