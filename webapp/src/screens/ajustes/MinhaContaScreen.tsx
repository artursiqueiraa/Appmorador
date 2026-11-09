import React, { useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, KeyRound, Save, Trash2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { Alert } from '../../components/DialogoAlerta';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

/**
 * Sprint 25 (ADR 0041) — o próprio usuário: dados, senha e excluir a conta (anonimizada, LGPD).
 */
export function MinhaContaScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { logout } = useAuth();
  const [nome, setNome] = useState('');
  const [email, setEmail] = useState('');
  const [telefone, setTelefone] = useState('');
  const [senhaAtual, setSenhaAtual] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [senhaExcluir, setSenhaExcluir] = useState('');
  const [mensagem, setMensagem] = useState<{ texto: string; erro: boolean } | null>(null);
  const [ocupado, setOcupado] = useState<'dados' | 'senha' | 'excluir' | null>(null);

  useEffect(() => {
    api
      .get<{ nome: string; email: string; telefone?: string | null }>('/api/minha-conta')
      .then((c) => {
        setNome(c.nome);
        setEmail(c.email);
        setTelefone(c.telefone ?? '');
      })
      .catch(() => setMensagem({ texto: 'Não foi possível carregar sua conta.', erro: true }));
  }, []);

  const executar = async (qual: 'dados' | 'senha' | 'excluir', acao: () => Promise<unknown>, sucesso: string) => {
    setOcupado(qual);
    setMensagem(null);
    try {
      await acao();
      setMensagem({ texto: sucesso, erro: false });
      return true;
    } catch (e) {
      setMensagem({ texto: e instanceof ApiError ? e.message : 'Não foi possível concluir agora.', erro: true });
      return false;
    } finally {
      setOcupado(null);
    }
  };

  const excluir = () =>
    Alert.alert(
      'Excluir sua conta?',
      'Seu nome, e-mail e telefone serão apagados e você sai do app. O histórico de segurança das propriedades continua, sem identificar você. Não dá para desfazer.',
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Excluir conta',
          style: 'destructive',
          onPress: async () => {
            const ok = await executar('excluir', () => api.post('/api/minha-conta/excluir', { senha: senhaExcluir }), 'Conta excluída.');
            if (ok) {
              logout();
            }
          },
        },
      ],
    );

  return (
    <ScrollView style={styles.tela} contentContainerStyle={styles.conteudo} keyboardShouldPersistTaps="handled">
      <View style={styles.cabecalho}>
        <Pressable onPress={() => navigation.goBack()} style={styles.voltar} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <Text style={styles.titulo}>Minha conta</Text>
      </View>

      {mensagem ? <Text style={[styles.mensagem, mensagem.erro && styles.erro]}>{mensagem.texto}</Text> : null}

      <Text style={styles.secao}>Seus dados</Text>
      <Text style={styles.detalhe}>{email}</Text>
      <TextField label="Nome" value={nome} onChangeText={setNome} autoCapitalize="words" />
      <TextField label="Celular com DDD (opcional)" value={telefone} onChangeText={setTelefone} keyboardType="phone-pad" />
      <PrimaryButton
        label="Salvar dados"
        icone={Save}
        loading={ocupado === 'dados'}
        onPress={() => executar('dados', () => api.put('/api/minha-conta', { nome, telefone }), 'Dados salvos.')}
      />

      <Text style={styles.secao}>Trocar senha</Text>
      <TextField label="Senha atual" value={senhaAtual} onChangeText={setSenhaAtual} secureTextEntry />
      <TextField label="Nova senha (mínimo 8 caracteres)" value={novaSenha} onChangeText={setNovaSenha} secureTextEntry />
      <PrimaryButton
        label="Trocar senha"
        icone={KeyRound}
        variant="secondary"
        loading={ocupado === 'senha'}
        disabled={!senhaAtual || novaSenha.length < 8}
        onPress={async () => {
          const ok = await executar('senha', () => api.put('/api/minha-conta/senha', { senhaAtual, novaSenha }), 'Senha trocada. Outros aparelhos vão pedir login de novo.');
          if (ok) {
            setSenhaAtual('');
            setNovaSenha('');
          }
        }}
      />

      <Text style={styles.secao}>Excluir conta</Text>
      <Text style={styles.detalhe}>
        Apaga seus dados pessoais (LGPD). Se você é dono de uma propriedade com outras pessoas, remova-as antes.
      </Text>
      <TextField label="Confirme com sua senha" value={senhaExcluir} onChangeText={setSenhaExcluir} secureTextEntry />
      <PrimaryButton label="Excluir minha conta" icone={Trash2} variant="danger" loading={ocupado === 'excluir'} disabled={!senhaExcluir} onPress={excluir} />
    </ScrollView>
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
  detalhe: { color: colors.sub, fontSize: fontSize.secondary, marginBottom: spacing.xs },
  mensagem: { color: colors.safe, fontSize: fontSize.secondary },
  erro: { color: colors.danger },
});
