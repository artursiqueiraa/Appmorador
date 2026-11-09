import React, { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { CircleCheck, Fingerprint, ScanFace, UserPlus, UserRound } from 'lucide-react-native';
import { ApiError } from '../../api/client';
import type { MoradorResponse } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Skeleton } from '../../components/Skeleton';
import { TelaCabecalho } from '../../components/neon/TelaCabecalho';
import { AvatarPessoa } from '../../moradores/AvatarPessoa';
import { CapturaFacial } from '../../moradores/CapturaFacial';
import { ConsentimentoFacial } from '../../moradores/ConsentimentoFacial';
import { descreverPapel } from '../../moradores/rotulosPessoas';
import { servicoPessoas } from '../../moradores/servicoPessoas';
import { useMoradores } from '../../moradores/useMoradores';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

const normalizar = (s: string) => s.trim().toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');

/**
 * Akteck Seg (ADR 0038) — "Iniciar cadastro facial" vai direto para a captura do rosto. Primeiro
 * a pessoa escolhe de quem é o rosto (pendentes primeiro); "Eu" cria o registro de titular do
 * próprio usuário na hora, se ele ainda não existir, sem passar pelo formulário de morador.
 */
export function CadastroFacialScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<RouteProp<RootStackParamList, 'CadastroFacial'>>();
  const { user } = useAuth();
  const m = useMoradores();
  const [pessoaId, setPessoaId] = useState<string | null>(params?.moradorId ?? null);
  const [criandoEu, setCriandoEu] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [concluida, setConcluida] = useState<MoradorResponse | null>(null);

  const pessoas = [...(m.pessoas ?? [])].sort((a, b) => Number(a.temFoto) - Number(b.temFoto) || a.nome.localeCompare(b.nome));
  const pessoa = pessoas.find((p) => p.id === pessoaId) ?? null;
  const nomeUsuario = user?.nome?.trim() ?? '';
  const euJaCadastrado = pessoas.some((p) => normalizar(p.nome) === normalizar(nomeUsuario));

  const escolherEu = async () => {
    setCriandoEu(true);
    setErro(null);
    try {
      const unidadeId = await m.garantirUnidade();
      const criada = await servicoPessoas.criarPessoa(unidadeId, { nome: nomeUsuario, tipo: 'Titular' });
      m.setPessoas([...(m.pessoas ?? []), criada]);
      setPessoaId(criada.id);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível preparar o seu cadastro. Tente de novo.');
    } finally {
      setCriandoEu(false);
    }
  };

  const recomecar = () => {
    setConcluida(null);
    setPessoaId(null);
  };

  if (concluida) {
    return (
      <ScrollView style={styles.tela} contentContainerStyle={styles.content}>
        <TelaCabecalho titulo="Cadastro facial" onVoltar={() => navigation.goBack()} />
        <View style={styles.sucesso}>
          <AvatarPessoa pessoa={concluida} tamanho={110} versao={1} />
          <View style={styles.sucessoLinha}>
            <CircleCheck size={iconSize.md} color={colors.safe} />
            <Text style={styles.sucessoTitulo}>Rosto cadastrado</Text>
          </View>
          <Text style={styles.sucessoTexto}>O rosto de {concluida.nome.split(' ')[0]} está salvo com segurança.</Text>
        </View>
        <PrimaryButton label="Cadastrar outro rosto" icone={ScanFace} onPress={recomecar} />
        <PrimaryButton label="Concluir" variant="secondary" onPress={() => navigation.goBack()} />
      </ScrollView>
    );
  }

  if (pessoa) {
    return (
      <ScrollView style={styles.tela} contentContainerStyle={styles.content}>
        <TelaCabecalho
          titulo={pessoa.temFoto ? `Trocar o rosto de ${pessoa.nome.split(' ')[0]}` : `Rosto de ${pessoa.nome.split(' ')[0]}`}
          subtitulo="Posicione o rosto no enquadramento, de frente e com boa luz."
          onVoltar={() => (params?.moradorId ? navigation.goBack() : setPessoaId(null))}
        />
        <ConsentimentoFacial moradorId={pessoa.id} nome={pessoa.nome}>
          <CapturaFacial
            moradorId={pessoa.id}
            onSalvo={() => {
              const atualizada = { ...pessoa, temFoto: true };
              m.setPessoas((m.pessoas ?? []).map((p) => (p.id === pessoa.id ? atualizada : p)));
              setConcluida(atualizada);
            }}
          />
        </ConsentimentoFacial>
      </ScrollView>
    );
  }

  return (
    <ScrollView style={styles.tela} contentContainerStyle={styles.content}>
      <TelaCabecalho icone={ScanFace} titulo="Cadastro facial" subtitulo="De quem é o rosto que você vai cadastrar?" onVoltar={() => navigation.goBack()} />

      {m.carregando ? (
        <View style={styles.lista}>
          <Skeleton height={64} radius={radius.lg} />
          <Skeleton height={64} radius={radius.lg} />
        </View>
      ) : (
        <View style={styles.lista}>
          {!euJaCadastrado && nomeUsuario ? (
            <Pressable onPress={escolherEu} disabled={criandoEu} style={[styles.opcao, styles.opcaoDestaque]} accessibilityRole="button">
              <View style={styles.opcaoIcone}>
                <UserRound size={iconSize.md} color={colors.safe} />
              </View>
              <View style={styles.opcaoTexto}>
                <Text style={styles.opcaoNome}>Eu, {nomeUsuario.split(' ')[0]}</Text>
                <Text style={styles.opcaoMeta}>{criandoEu ? 'Preparando o seu cadastro…' : 'Cadastrar o meu próprio rosto'}</Text>
              </View>
            </Pressable>
          ) : null}

          {pessoas.map((p) => (
            <Pressable key={p.id} onPress={() => setPessoaId(p.id)} style={styles.opcao} accessibilityRole="button" accessibilityLabel={`${p.nome}, ${p.temFoto ? 'rosto cadastrado' : 'rosto pendente'}`}>
              <AvatarPessoa pessoa={p} tamanho={42} />
              <View style={styles.opcaoTexto}>
                <Text style={styles.opcaoNome}>{p.nome}</Text>
                <Text style={styles.opcaoMeta}>{descreverPapel(p)}</Text>
              </View>
              <View style={[styles.selo, p.temFoto ? styles.seloOk : styles.seloPendente]}>
                {p.temFoto ? <Fingerprint size={12} color={colors.safe} /> : null}
                <Text style={[styles.seloTexto, { color: p.temFoto ? colors.safe : colors.warn }]}>{p.temFoto ? 'Trocar' : 'Pendente'}</Text>
              </View>
            </Pressable>
          ))}

          {erro ? <Text style={styles.erro}>{erro}</Text> : null}

          <Pressable
            onPress={async () => navigation.replace('NovaPessoa', { unidadeId: await m.garantirUnidade() })}
            style={styles.outra}
            accessibilityRole="button"
          >
            <UserPlus size={16} color={colors.sub} />
            <Text style={styles.outraTexto}>É de alguém que ainda não está cadastrado</Text>
          </Pressable>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tela: { flex: 1, backgroundColor: colors.bg },
  content: { paddingHorizontal: 18, paddingBottom: spacing.xxl, gap: spacing.md },
  lista: { gap: spacing.sm },
  opcao: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 66,
    paddingVertical: spacing.md,
    paddingHorizontal: 14,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  opcaoDestaque: { borderColor: colors.safeLine, backgroundColor: colors.surface2 },
  opcaoIcone: { width: 42, height: 42, borderRadius: 21, backgroundColor: colors.safeDim, alignItems: 'center', justifyContent: 'center' },
  opcaoTexto: { flex: 1, minWidth: 0 },
  opcaoNome: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  opcaoMeta: { color: colors.mute, fontSize: fontSize.meta, marginTop: 2 },
  selo: { flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: 9, paddingVertical: 5, borderRadius: radius.sm },
  seloOk: { backgroundColor: colors.safeDim },
  seloPendente: { backgroundColor: colors.warnDim },
  seloTexto: { fontSize: fontSize.tiny, fontWeight: fontWeight.bold },
  outra: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: spacing.sm, minHeight: 48, marginTop: spacing.sm },
  outraTexto: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  erro: { color: colors.danger, fontSize: fontSize.secondary, textAlign: 'center' },
  sucesso: { alignItems: 'center', gap: spacing.sm, paddingVertical: spacing.xl },
  sucessoLinha: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, marginTop: spacing.md },
  sucessoTitulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  sucessoTexto: { color: colors.sub, fontSize: fontSize.body, textAlign: 'center' },
});
