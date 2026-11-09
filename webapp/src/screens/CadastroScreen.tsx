import React, { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Lock, LogIn, Mail, User, UserPlus } from 'lucide-react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { RootStackParamList } from '../navigation/types';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import { PrimaryButton } from '../components/PrimaryButton';
import { MarcaAkteck } from '../components/marca/MarcaAkteck';
import { CartaoNeon } from '../components/neon/CartaoNeon';
import { IconeQuadrado } from '../components/neon/IconeQuadrado';
import { TextField } from '../components/TextField';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

type Props = NativeStackScreenProps<RootStackParamList, 'Cadastro'>;

export function CadastroScreen({ navigation }: Props) {
  const { register } = useAuth();
  const [nome, setNome] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleRegister = async () => {
    setError(null);
    setLoading(true);
    try {
      await register(nome.trim(), email.trim(), password);
      setSuccess(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível concluir o cadastro.');
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <View style={styles.container}>
        <View style={styles.content}>
          <View style={styles.iconWrap}>
            <UserPlus size={32} color={colors.marca} />
          </View>
          <Text style={styles.title}>Conta criada!</Text>
          <Text style={styles.subtitle}>Agora entre com o e-mail e a senha que você cadastrou.</Text>
          <PrimaryButton label="Ir para o login" onPress={() => navigation.navigate('Login')} />
        </View>
      </View>
    );
  }

  return (
    <KeyboardAvoidingView style={styles.container} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
        {/* Akteck Seg (ADR 0037) — mesmo padrão visual do login (marca + cartão com campos com ícone). */}
        <View style={styles.marcaWrap}>
          <MarcaAkteck variante="empilhada" tamanho={34} />
        </View>
        <CartaoNeon style={styles.cartao}>
        <View style={styles.cartaoTopo}>
          <IconeQuadrado icone={UserPlus} tamanho={60} />
          <View style={styles.cartaoTitulo}>
            <Text style={styles.title}>Criar conta</Text>
            <Text style={styles.subtitle}>Leva menos de um minuto.</Text>
          </View>
        </View>

        <TextField label="Nome" icone={User} value={nome} onChangeText={setNome} placeholder="Seu nome" />
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
        <TextField
          label="Senha"
          icone={Lock}
          value={password}
          onChangeText={setPassword}
          secureTextEntry
          placeholder="Mín. 8 caracteres, com maiúscula, minúscula e número"
        />

        {error ? <Text style={styles.error}>{error}</Text> : null}

        <PrimaryButton label="Criar conta" onPress={handleRegister} loading={loading} disabled={!nome || !email || !password} />
        <PrimaryButton label="Já tenho conta" icone={LogIn} variant="secondary" onPress={() => navigation.navigate('Login')} />
        </CartaoNeon>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  marcaWrap: { alignItems: 'center', marginBottom: spacing.xxl },
  cartao: { padding: spacing.xxl, borderRadius: radius.xxl },
  cartaoTopo: { flexDirection: 'row', alignItems: 'center', gap: spacing.lg, marginBottom: spacing.xl },
  cartaoTitulo: { flex: 1, minWidth: 0 },
  container: { flex: 1, backgroundColor: colors.bg },
  content: { flexGrow: 1, justifyContent: 'center', padding: spacing.xxl },
  iconWrap: {
    width: 64,
    height: 64,
    borderRadius: 999,
    backgroundColor: colors.marcaDim,
    borderWidth: 1.5,
    borderColor: colors.marca,
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'center',
    marginBottom: spacing.lg,
  },
  title: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold, textAlign: 'center' },
  subtitle: {
    color: colors.sub,
    fontSize: fontSize.secondary,
    textAlign: 'center',
    marginTop: spacing.xs,
    marginBottom: spacing.xxl,
  },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
});
