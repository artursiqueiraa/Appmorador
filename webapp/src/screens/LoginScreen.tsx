import React, { useState } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Building2, Cctv, LogIn, Lock, Mail, ShieldCheck, UserPlus, Users } from 'lucide-react-native';
import type { RootStackParamList } from '../navigation/types';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import { Alert } from '../components/DialogoAlerta';
import { PrimaryButton } from '../components/PrimaryButton';
import { MarcaAkteck } from '../components/marca/MarcaAkteck';
import { CartaoNeon } from '../components/neon/CartaoNeon';
import { Degrade } from '../components/neon/Degrade';
import { IconeQuadrado } from '../components/neon/IconeQuadrado';
import { TextField } from '../components/TextField';
import { colors, efeitos, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';

type Props = NativeStackScreenProps<RootStackParamList, 'Login'>;

const DESTAQUES = [
  { icone: ShieldCheck, rotulo: 'Monitoramento\n24h' },
  { icone: Cctv, rotulo: 'Câmeras\ne DVRs' },
  { icone: Users, rotulo: 'Gestão\nde Acessos' },
];

/**
 * Akteck Seg (ADR 0037) — login no padrão visual oficial (referência do usuário): marca empilhada,
 * faixa de destaques com cantos verdes e cartão "Acesso do Proprietário". A lógica de login é a
 * mesma de antes.
 */
export function LoginScreen({ navigation }: Props) {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    setError(null);
    setLoading(true);
    try {
      await login(email.trim(), password);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível entrar. Tente novamente.');
    } finally {
      setLoading(false);
    }
  };

  // Ainda não existe recuperação de senha por e-mail no sistema: a tela diz isso em vez de fingir um envio.
  const esqueciSenha = () =>
    Alert.alert('Esqueceu sua senha?', 'Fale com o atendimento da Akteck Seg para redefinir o acesso com segurança. A recuperação pelo app está chegando.');

  return (
    <KeyboardAvoidingView style={styles.container} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <Degrade cores={efeitos.degradeFundo} />
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
        <View style={styles.marcaWrap}>
          <MarcaAkteck variante="empilhada" tamanho={40} />
        </View>

        <View style={styles.destaques}>
          <View style={[styles.canto, styles.cantoEsquerdo]} />
          {DESTAQUES.map((d, i) => (
            <View key={d.rotulo} style={[styles.destaque, i > 0 && styles.destaqueDivisor]}>
              <d.icone size={iconSize.lg} color={colors.marca} />
              <Text style={styles.destaqueTexto}>{d.rotulo}</Text>
            </View>
          ))}
          <View style={[styles.canto, styles.cantoDireito]} />
        </View>

        <CartaoNeon style={styles.cartao}>
          <View style={styles.cartaoTopo}>
            <IconeQuadrado icone={Building2} tamanho={64} />
            <View style={styles.cartaoTitulo}>
              <Text style={styles.title} accessibilityRole="header">
                Acesso do Proprietário
              </Text>
              <Text style={styles.subtitle}>Acesse sua conta e gerencie a segurança da sua propriedade.</Text>
            </View>
          </View>

          <TextField
            label="E-mail"
            icone={Mail}
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="email-address"
            placeholder="voce@exemplo.com"
          />
          <TextField label="Senha" icone={Lock} value={password} onChangeText={setPassword} secureTextEntry placeholder="Sua senha" />

          <Pressable onPress={esqueciSenha} style={styles.esqueci} accessibilityRole="button">
            <Text style={styles.esqueciTexto}>Esqueceu sua senha?</Text>
          </Pressable>

          {error ? <Text style={styles.error}>{error}</Text> : null}

          <PrimaryButton label="Entrar" icone={LogIn} onPress={handleLogin} loading={loading} disabled={!email || !password} />
          <PrimaryButton label="Criar conta" icone={UserPlus} variant="secondary" onPress={() => navigation.navigate('Cadastro')} />
        </CartaoNeon>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { flexGrow: 1, justifyContent: 'center', padding: spacing.xl, gap: spacing.xxl },
  marcaWrap: { alignItems: 'center', marginTop: spacing.lg },
  destaques: { flexDirection: 'row', paddingVertical: spacing.lg, paddingHorizontal: spacing.sm },
  canto: { position: 'absolute', top: 0, bottom: 0, width: 18, borderColor: colors.marca, borderTopWidth: 2, borderBottomWidth: 2 },
  cantoEsquerdo: { left: 0, borderLeftWidth: 2, borderTopLeftRadius: radius.md, borderBottomLeftRadius: radius.md },
  cantoDireito: { right: 0, borderRightWidth: 2, borderTopRightRadius: radius.md, borderBottomRightRadius: radius.md },
  destaque: { flex: 1, alignItems: 'center', gap: spacing.sm },
  destaqueDivisor: { borderLeftWidth: 1, borderLeftColor: colors.line },
  destaqueTexto: { color: colors.text, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, textAlign: 'center' },
  cartao: { padding: spacing.xxl, borderRadius: radius.xxl, boxShadow: `${efeitos.brilhoSuave}, 0 -1px 18px rgba(0,230,154,0.18)` },
  cartaoTopo: { flexDirection: 'row', alignItems: 'center', gap: spacing.lg, marginBottom: spacing.xxl },
  cartaoTitulo: { flex: 1, minWidth: 0 },
  title: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  subtitle: { color: colors.sub, fontSize: fontSize.body, marginTop: spacing.xs },
  esqueci: { alignSelf: 'flex-end', minHeight: 40, justifyContent: 'center', marginTop: -spacing.sm },
  esqueciTexto: { color: colors.marca, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  error: { color: colors.danger, fontSize: fontSize.secondary, textAlign: 'center', marginBottom: spacing.sm },
});
