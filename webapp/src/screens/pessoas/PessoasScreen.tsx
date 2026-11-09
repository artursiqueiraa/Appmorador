import React, { useCallback, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Share, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, Clock, Send, Trash2, UserPlus, UserRound } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { PerfilPropriedade } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Alert } from '../../components/DialogoAlerta';
import { OpcoesChips } from '../../components/OpcoesChips';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import type { RootStackParamList } from '../../navigation/types';
import { PAPEIS, PAPEIS_CONVIDAVEIS } from '../../pessoas/papeis';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

interface Pessoa {
  vinculoId?: string | null;
  usuarioId: string;
  nome: string;
  email: string;
  papel: PerfilPropriedade;
  ehDono: boolean;
}

interface ConvitePendente {
  id: string;
  email: string;
  papel: PerfilPropriedade;
  expiraEmUtc: string;
}

interface ConviteCriado {
  id: string;
  link: string;
  expiraEmUtc: string;
  emailEnviado: boolean;
}

/**
 * Sprint 25 (ADR 0041) — quem tem acesso a esta propriedade (só o Proprietário vê). Convidar manda
 * e-mail (Resend) e mostra o link para compartilhar no WhatsApp/SMS; trocar papel e remover valem
 * na próxima ação da pessoa.
 */
export function PessoasScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const propriedadeId = selectedProperty?.id ?? '';

  const [pessoas, setPessoas] = useState<Pessoa[] | null>(null);
  const [convites, setConvites] = useState<ConvitePendente[]>([]);
  const [erro, setErro] = useState<string | null>(null);
  const [email, setEmail] = useState('');
  const [papel, setPapel] = useState<PerfilPropriedade>('Morador');
  const [enviando, setEnviando] = useState(false);
  const [criado, setCriado] = useState<ConviteCriado | null>(null);

  const carregar = useCallback(async () => {
    try {
      const r = await api.get<{ pessoas: Pessoa[]; convitesPendentes: ConvitePendente[] }>(`/api/properties/${propriedadeId}/pessoas`);
      setPessoas(r.pessoas);
      setConvites(r.convitesPendentes);
      setErro(null);
    } catch (e) {
      setPessoas([]);
      setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar as pessoas.');
    }
  }, [propriedadeId]);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar]),
  );

  const convidar = async () => {
    setEnviando(true);
    setErro(null);
    try {
      const r = await api.post<ConviteCriado>(`/api/properties/${propriedadeId}/convites`, { email, papel });
      setCriado(r);
      setEmail('');
      carregar();
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível enviar o convite.');
    } finally {
      setEnviando(false);
    }
  };

  const compartilhar = (link: string) =>
    Share.share({ message: `Você foi convidado para acessar ${selectedProperty?.nome ?? 'minha propriedade'} no Akteck Seg: ${link}` }).catch(() => {});

  const mudarPapel = async (pessoa: Pessoa, novo: PerfilPropriedade) => {
    if (!pessoa.vinculoId) {
      return;
    }
    await api.put(`/api/pessoas/${pessoa.vinculoId}/papel`, { papel: novo }).catch(() => {});
    carregar();
  };

  const remover = (pessoa: Pessoa) =>
    Alert.alert('Remover acesso?', `${pessoa.nome} deixa de acessar esta propriedade na próxima ação no app.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          if (pessoa.vinculoId) {
            await api.delete(`/api/pessoas/${pessoa.vinculoId}`).catch(() => {});
            carregar();
          }
        },
      },
    ]);

  const cancelarConvite = async (c: ConvitePendente) => {
    await api.delete(`/api/convites/${c.id}`).catch(() => {});
    carregar();
  };

  return (
    <ScrollView style={styles.tela} contentContainerStyle={styles.conteudo} keyboardShouldPersistTaps="handled">
      <View style={styles.cabecalho}>
        <Pressable onPress={() => navigation.goBack()} style={styles.voltar} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={{ flex: 1 }}>
          <Text style={styles.titulo}>Pessoas com acesso</Text>
          <Text style={styles.subtitulo}>{selectedProperty?.nome}</Text>
        </View>
      </View>

      {erro ? <Text style={styles.erro}>{erro}</Text> : null}

      {pessoas === null ? (
        <ActivityIndicator color={colors.accent} />
      ) : (
        pessoas.map((p) => (
          <View key={p.usuarioId} style={styles.cartao}>
            <View style={styles.linha}>
              <View style={styles.icone}>
                <UserRound size={18} color={colors.sub} />
              </View>
              <View style={{ flex: 1, minWidth: 0 }}>
                <Text style={styles.nome} numberOfLines={1}>
                  {p.nome}
                </Text>
                <Text style={styles.detalhe} numberOfLines={1}>
                  {PAPEIS[p.papel]?.rotulo ?? p.papel} · {p.email}
                </Text>
              </View>
              {!p.ehDono ? (
                <Pressable onPress={() => remover(p)} style={styles.acao} accessibilityLabel={`Remover ${p.nome}`}>
                  <Trash2 size={16} color={colors.danger} />
                </Pressable>
              ) : null}
            </View>
            {!p.ehDono ? (
              <OpcoesChips
                rotulo="Papel"
                opcoes={PAPEIS_CONVIDAVEIS.map((v) => ({ valor: v, rotulo: PAPEIS[v].rotulo }))}
                valor={p.papel}
                onChange={(v) => mudarPapel(p, v)}
              />
            ) : null}
          </View>
        ))
      )}

      {convites.length > 0 ? <Text style={styles.secao}>Convites aguardando</Text> : null}
      {convites.map((c) => (
        <View key={c.id} style={[styles.cartao, styles.linha]}>
          <View style={styles.icone}>
            <Clock size={18} color={colors.sub} />
          </View>
          <View style={{ flex: 1, minWidth: 0 }}>
            <Text style={styles.nome} numberOfLines={1}>
              {c.email}
            </Text>
            <Text style={styles.detalhe}>
              {PAPEIS[c.papel]?.rotulo} · vale até {new Date(c.expiraEmUtc).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
            </Text>
          </View>
          <Pressable onPress={() => cancelarConvite(c)} style={styles.acao} accessibilityLabel={`Cancelar convite de ${c.email}`}>
            <Trash2 size={16} color={colors.danger} />
          </Pressable>
        </View>
      ))}

      <Text style={styles.secao}>Convidar alguém</Text>
      <TextField label="E-mail da pessoa" value={email} onChangeText={setEmail} placeholder="nome@exemplo.com" keyboardType="email-address" autoCapitalize="none" />
      <OpcoesChips
        rotulo="Papel"
        opcoes={PAPEIS_CONVIDAVEIS.map((v) => ({ valor: v, rotulo: PAPEIS[v].rotulo }))}
        valor={papel}
        onChange={setPapel}
        dica={PAPEIS[papel].descricao}
      />
      <PrimaryButton label="Enviar convite" icone={UserPlus} onPress={convidar} loading={enviando} disabled={!email.includes('@')} />

      {criado ? (
        <View style={[styles.cartao, styles.criado]}>
          <Text style={styles.nome}>{criado.emailEnviado ? 'Convite enviado por e-mail.' : 'Convite criado.'}</Text>
          <Text style={styles.detalhe}>
            {criado.emailEnviado ? 'Se preferir, mande também pelo WhatsApp:' : 'Mande o link pelo WhatsApp ou SMS — o e-mail ainda não está ativo:'}
          </Text>
          <Text style={styles.link} selectable>
            {criado.link}
          </Text>
          <PrimaryButton label="Compartilhar link" icone={Send} variant="secondary" onPress={() => compartilhar(criado.link)} />
        </View>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tela: { flex: 1, backgroundColor: colors.bg },
  conteudo: { padding: spacing.xl, gap: spacing.md, paddingBottom: spacing.xxxl },
  cabecalho: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
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
  subtitulo: { color: colors.sub, fontSize: fontSize.secondary },
  secao: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold, marginTop: spacing.lg },
  cartao: { padding: spacing.md, borderRadius: radius.lg, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line, gap: spacing.sm },
  criado: { borderColor: colors.marca },
  linha: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
  icone: { width: 36, height: 36, borderRadius: radius.md, backgroundColor: colors.surface2, alignItems: 'center', justifyContent: 'center' },
  nome: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  detalhe: { color: colors.mute, fontSize: fontSize.meta, marginTop: 2 },
  link: { color: colors.marca, fontSize: fontSize.meta },
  acao: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  erro: { color: colors.danger, fontSize: fontSize.secondary },
});
