import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Camera, ChevronRight, Plus, QrCode, ScanFace, UserPlus, Users } from 'lucide-react-native';
import { api } from '../../api/client';
import type { VisitanteResponse } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import { useLayoutDesktop } from '../../layout/useLayoutDesktop';
import { ListaPessoas } from '../../moradores/ListaPessoas';
import { PainelPessoa } from '../../moradores/PainelPessoa';
import { useMoradores } from '../../moradores/useMoradores';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, iconSize, layout, radius, spacing } from '../../theme/theme';

type Aba = 'moradores' | 'provisorios';

/** Cantos de enquadramento do cartão "Cadastrar rosto" (protótipo). */
function Moldura() {
  return (
    <View style={styles.moldura}>
      {[styles.cantoSE, styles.cantoSD, styles.cantoIE, styles.cantoID].map((canto, i) => (
        <View key={i} style={[styles.canto, canto]} />
      ))}
      <ScanFace size={30} color={colors.safe} />
    </View>
  );
}

/**
 * Akteck Seg (ADR 0038) — aba "Moradores" (a tela "Acessos" do protótipo): o cartão de cadastro
 * facial como âncora e as abas Moradores / Provisórios. Portões, vagas e entregas saíram daqui
 * (abrir portão foi para o Início; o resto, para o menu). No computador, lista e detalhe da
 * pessoa ficam lado a lado.
 */
export function AccessScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const desktop = useLayoutDesktop();
  const m = useMoradores();
  const [aba, setAba] = useState<Aba>('moradores');
  const [selecionadaId, setSelecionadaId] = useState<string | null>(null);
  const [visitantes, setVisitantes] = useState<VisitanteResponse[] | null>(null);
  const [abrindo, setAbrindo] = useState(false);

  const pessoas = useMemo(
    () => [...(m.pessoas ?? [])].sort((a, b) => (a.tipo === b.tipo ? a.nome.localeCompare(b.nome) : a.tipo === 'Titular' ? -1 : 1)),
    [m.pessoas],
  );
  const semRosto = pessoas.find((p) => !p.temFoto);
  const selecionada = pessoas.find((p) => p.id === selecionadaId) ?? null;

  useFocusEffect(
    useCallback(() => {
      if (!selectedProperty) return;
      api
        .get<VisitanteResponse[]>(`/api/properties/${selectedProperty.id}/visitantes`)
        .then(setVisitantes)
        .catch(() => setVisitantes([]));
    }, [selectedProperty]),
  );

  useEffect(() => {
    if (desktop && aba === 'moradores' && !selecionada && pessoas.length > 0) setSelecionadaId(pessoas[0].id);
  }, [desktop, aba, selecionada, pessoas]);

  const adicionarPessoa = async () => {
    setAbrindo(true);
    try {
      navigation.navigate('NovaPessoa', { unidadeId: await m.garantirUnidade() });
    } finally {
      setAbrindo(false);
    }
  };

  const abrirPessoa = (id: string, unidadeId: string) => (desktop ? setSelecionadaId(id) : navigation.navigate('Pessoa', { moradorId: id, unidadeId }));

  // "Iniciar cadastro facial" vai direto para a captura do rosto (escolhendo de quem é antes).
  const iniciarFacial = () => navigation.navigate('CadastroFacial');

  if (!selectedProperty) return null;

  const listaMoradores = m.carregando ? (
    <View style={styles.lista}>
      {[0, 1, 2].map((i) => (
        <Skeleton key={i} height={66} radius={radius.lg} />
      ))}
    </View>
  ) : (
    <View style={styles.lista}>
      {pessoas.length === 0 ? (
        <EstadoVazio icon={Users} titulo="Ninguém cadastrado ainda" descricao="Cadastre quem mora com você para liberar o acesso facial e os veículos de cada um." />
      ) : (
        <ListaPessoas pessoas={pessoas} selecionadaId={desktop ? selecionadaId : null} onSelecionar={(p) => abrirPessoa(p.id, p.unidadeId)} />
      )}
      <Pressable onPress={adicionarPessoa} disabled={abrindo} style={styles.botaoAdd} accessibilityRole="button">
        <UserPlus size={16} color={colors.text} />
        <Text style={styles.botaoAddTexto}>Adicionar pessoa</Text>
      </Pressable>
    </View>
  );

  const listaProvisorios = (
    <View style={styles.lista}>
      {visitantes === null ? (
        <Skeleton height={66} radius={radius.lg} />
      ) : visitantes.length === 0 ? (
        <EstadoVazio icon={QrCode} titulo="Nenhum acesso provisório" descricao="Libere a entrada de visitantes, prestadores e entregadores por um período." />
      ) : (
        visitantes.map((v) => (
          <Pressable
            key={v.id}
            onPress={() => navigation.navigate('Autorizacoes', { visitanteId: v.id, nomeVisitante: v.nome, propriedadeId: selectedProperty.id })}
            style={styles.provisorio}
            accessibilityRole="button"
          >
            <View style={styles.qr}>
              <QrCode size={28} color={colors.bg} />
            </View>
            <View style={styles.provisorioTexto}>
              <Text style={styles.provisorioNome}>{v.nome}</Text>
              <Text style={styles.provisorioMeta}>Ver autorizações de entrada</Text>
            </View>
            <ChevronRight size={iconSize.sm} color={colors.mute} />
          </Pressable>
        ))
      )}
      <Pressable
        onPress={() => navigation.navigate('Visitantes', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome })}
        style={[styles.botaoAdd, styles.botaoProvisorio]}
        accessibilityRole="button"
      >
        <Plus size={16} color={colors.bg} />
        <Text style={[styles.botaoAddTexto, { color: colors.bg }]}>Gerar acesso provisório</Text>
      </Pressable>
    </View>
  );

  const coluna = (
    <>
      <View style={styles.cartaoFacial}>
        <View style={styles.brilho} />
        <View style={styles.cartaoFacialLinha}>
          <Moldura />
          <View style={styles.cartaoFacialTexto}>
            <Text style={styles.cartaoFacialTitulo}>{semRosto ? `Falta o rosto de ${semRosto.nome.split(' ')[0]}` : 'Cadastrar rosto'}</Text>
            <Text style={styles.cartaoFacialSub}>Use a câmera do celular ou do computador. A foto fica salva com segurança para o acesso facial.</Text>
          </View>
        </View>
        <Pressable onPress={iniciarFacial} style={styles.botaoFacial} accessibilityRole="button">
          <Camera size={17} color={colors.onMarca} />
          <Text style={styles.botaoFacialTexto}>Iniciar cadastro facial</Text>
        </Pressable>
      </View>

      <View style={styles.abas} accessibilityRole="tablist">
        {(
          [
            ['moradores', `Moradores (${pessoas.length})`],
            ['provisorios', `Provisórios (${visitantes?.length ?? 0})`],
          ] as const
        ).map(([id, rotulo]) => (
          <Pressable key={id} onPress={() => setAba(id)} style={[styles.aba, aba === id && styles.abaAtiva]} accessibilityRole="tab" accessibilityState={{ selected: aba === id }}>
            <Text style={[styles.abaTexto, aba === id && styles.abaTextoAtiva]}>{rotulo}</Text>
          </Pressable>
        ))}
      </View>

      {aba === 'moradores' ? listaMoradores : listaProvisorios}
    </>
  );

  return (
    <ScrollView style={styles.tela} contentContainerStyle={[styles.content, desktop && styles.contentDesktop]}>
      <View style={styles.cabecalho}>
        <Text style={styles.titulo}>Acessos</Text>
        <Text style={styles.subtitulo}>Quem pode entrar e como</Text>
      </View>

      {desktop && aba === 'moradores' && selecionada ? (
        <View style={styles.duasColunas}>
          <View style={styles.colunaLista}>{coluna}</View>
          <View style={styles.colunaDetalhe}>
            <PainelPessoa
              key={selecionada.id}
              pessoa={selecionada}
              onAlterada={(p) => m.setPessoas((m.pessoas ?? []).map((x) => (x.id === p.id ? p : x)))}
              onRemovida={() => {
                setSelecionadaId(null);
                m.recarregar();
              }}
              onAbrirCredenciais={() => navigation.navigate('Credenciais', { moradorId: selecionada.id, nomeMorador: selecionada.nome, propriedadeId: selectedProperty.id })}
            />
          </View>
        </View>
      ) : (
        <View style={desktop ? styles.colunaUnica : undefined}>{coluna}</View>
      )}
    </ScrollView>
  );
}

const CANTO = 14;

const styles = StyleSheet.create({
  tela: { flex: 1, backgroundColor: colors.bg },
  content: { paddingHorizontal: 18, paddingBottom: spacing.xxl },
  contentDesktop: { paddingHorizontal: spacing.xxl, paddingTop: spacing.lg },
  cabecalho: { paddingTop: spacing.sm, paddingBottom: spacing.lg },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  subtitulo: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 3 },
  cartaoFacial: { borderRadius: 20, padding: 20, marginBottom: 20, backgroundColor: colors.surface2, borderWidth: 1, borderColor: colors.line, overflow: 'hidden' },
  brilho: { position: 'absolute', right: -20, top: -20, width: 130, height: 130, borderRadius: 65, backgroundColor: colors.safeDim },
  cartaoFacialLinha: { flexDirection: 'row', alignItems: 'center', gap: spacing.lg },
  moldura: { width: 66, height: 66, alignItems: 'center', justifyContent: 'center' },
  canto: { position: 'absolute', width: CANTO, height: CANTO, borderColor: colors.safe, borderRadius: 3 },
  cantoSE: { top: 0, left: 0, borderTopWidth: 2, borderLeftWidth: 2 },
  cantoSD: { top: 0, right: 0, borderTopWidth: 2, borderRightWidth: 2 },
  cantoIE: { bottom: 0, left: 0, borderBottomWidth: 2, borderLeftWidth: 2 },
  cantoID: { bottom: 0, right: 0, borderBottomWidth: 2, borderRightWidth: 2 },
  cartaoFacialTexto: { flex: 1, minWidth: 0 },
  cartaoFacialTitulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  cartaoFacialSub: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 4, lineHeight: 18 },
  botaoFacial: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: spacing.sm, marginTop: spacing.lg, minHeight: 48, borderRadius: 13, backgroundColor: colors.safe },
  botaoFacialTexto: { color: colors.onMarca, fontSize: fontSize.body, fontWeight: fontWeight.black },
  abas: { flexDirection: 'row', gap: 6, padding: 4, borderRadius: radius.md, backgroundColor: colors.bg2, borderWidth: 1, borderColor: colors.lineSoft },
  aba: { flex: 1, minHeight: 40, borderRadius: 9, alignItems: 'center', justifyContent: 'center' },
  abaAtiva: { backgroundColor: colors.surface2 },
  abaTexto: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.bold },
  abaTextoAtiva: { color: colors.text },
  lista: { marginTop: 14, gap: spacing.sm },
  botaoAdd: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    minHeight: 48,
    marginTop: 4,
    borderRadius: 13,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  botaoAddTexto: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  botaoProvisorio: { backgroundColor: colors.accent, borderWidth: 0 },
  provisorio: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, padding: spacing.lg, borderRadius: radius.lg, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line },
  qr: { width: 46, height: 46, borderRadius: 12, backgroundColor: colors.text, alignItems: 'center', justifyContent: 'center' },
  provisorioTexto: { flex: 1, minWidth: 0 },
  provisorioNome: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  provisorioMeta: { color: colors.accent, fontSize: fontSize.meta, fontWeight: fontWeight.medium, marginTop: 4 },
  duasColunas: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.xxl },
  colunaLista: { width: layout.listaMestreLargura + 40 },
  colunaDetalhe: { flex: 1, minWidth: 0 },
  colunaUnica: { maxWidth: 640 },
});
