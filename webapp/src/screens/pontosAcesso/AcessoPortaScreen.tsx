import React, { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Image, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { CheckCircle2, ChevronLeft, DoorOpen, RefreshCw, VideoOff, XCircle } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import { useImagemAutenticada } from '../../api/useImagemAutenticada';
import type { CameraSnapshotResponse, PontoAcessoResponse, ResultadoAberturaResponse } from '../../api/types';
import { usePermissao } from '../../auth/usePermissao';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { ServicoFeedbackTatil } from '../../services/ServicoFeedbackTatil';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';
import { confirmarComando } from '../../seguranca/confirmarComando';

type Rota = RouteProp<RootStackParamList, 'AcessoPorta'>;

/**
 * Sprint 24.5 — tela de um portão/porta: primeiro a imagem da câmera daquele acesso (a última
 * captura, com "Atualizar"), depois a decisão: **Abrir** ou **Agora não**. É também a proteção
 * contra abrir sem querer — só abre quem viu a porta e tocou em Abrir.
 */
export function AcessoPortaScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<Rota>();
  const { temPermissao } = usePermissao();
  const podeAbrir = temPermissao('AbrirPortao');

  const [ponto, setPonto] = useState<PontoAcessoResponse | null>(null);
  const [snapshot, setSnapshot] = useState<CameraSnapshotResponse | null>(null);
  const [carregandoImagem, setCarregandoImagem] = useState(false);
  const [abrindo, setAbrindo] = useState(false);
  const [resultado, setResultado] = useState<ResultadoAberturaResponse | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const imagem = useImagemAutenticada(snapshot?.ultimaImagemUrl ?? null, `${snapshot?.ultimaImagemUrl}-${snapshot?.capturadaEmUtc}`);

  const carregarSnapshot = useCallback(async (cameraId: string, capturarAgora: boolean) => {
    setCarregandoImagem(true);
    try {
      const resposta = capturarAgora
        ? await api.post<CameraSnapshotResponse>(`/api/cameras/${cameraId}/snapshot`)
        : await api.get<CameraSnapshotResponse | undefined>(`/api/cameras/${cameraId}/snapshot`);
      setSnapshot(resposta ?? null);
    } catch {
      setSnapshot(null);
    } finally {
      setCarregandoImagem(false);
    }
  }, []);

  useEffect(() => {
    let cancelado = false;
    api
      .get<PontoAcessoResponse[]>(`/api/properties/${params.propriedadeId}/pontos-acesso`)
      .then((lista) => {
        if (cancelado) {
          return;
        }
        const encontrado = lista.find((p) => p.id === params.pontoAcessoId) ?? null;
        setPonto(encontrado);
        if (encontrado?.cameraId) {
          carregarSnapshot(encontrado.cameraId, false);
        }
      })
      .catch((e) => setErro(e instanceof ApiError ? e.message : 'Não foi possível carregar este acesso.'));
    return () => {
      cancelado = true;
    };
  }, [params.propriedadeId, params.pontoAcessoId, carregarSnapshot]);

  const abrir = async () => {
    if (!ponto) {
      return;
    }
    setAbrindo(true);
    setResultado(null);
    try {
      // Sprint 27 (ADR 0043) — abrir pede o PIN ou a digital/rosto.
      const r = await confirmarComando(`Abrir ${ponto.nome}`, (cab) =>
        api.post<ResultadoAberturaResponse>(`/api/pontos-acesso/${ponto.id}/abrir`, undefined, cab),
      );
      if (r === null) {
        return;
      }
      setResultado(r);
      if (r.sucesso) {
        ServicoFeedbackTatil.impactMedium();
      } else {
        ServicoFeedbackTatil.notificationError();
      }
    } catch (e) {
      setResultado({ sucesso: false, mensagem: e instanceof ApiError ? e.message : 'Não foi possível abrir agora.' });
    } finally {
      setAbrindo(false);
    }
  };

  const nome = ponto?.nome ?? params.nome;

  return (
    <ScrollView style={styles.tela} contentContainerStyle={styles.conteudo}>
      <View style={styles.cabecalho}>
        <Pressable onPress={() => navigation.goBack()} style={styles.voltar} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={{ flex: 1 }}>
          <Text style={styles.titulo} numberOfLines={1}>
            {nome}
          </Text>
          {ponto?.nomeEquipamentoAcionador ? (
            <Text style={styles.subtitulo} numberOfLines={1}>
              {ponto.acionadorOnline ? 'Pronto para abrir' : 'Equipamento sem comunicação'} · {ponto.nomeEquipamentoAcionador}
            </Text>
          ) : null}
        </View>
      </View>

      {erro ? <Text style={styles.erro}>{erro}</Text> : null}

      {!ponto && !erro ? (
        <Skeleton height={240} radius={radius.xl} />
      ) : ponto?.cameraId ? (
        <View style={styles.imagemWrap}>
          {imagem ? (
            <Image source={{ uri: imagem }} style={styles.imagem} resizeMode="cover" accessibilityLabel={`Imagem de ${ponto.nomeCamera ?? 'câmera'}`} />
          ) : (
            <View style={styles.semImagem}>
              {carregandoImagem ? <ActivityIndicator color={colors.marca} /> : <VideoOff size={iconSize.xl} color={colors.mute} />}
              <Text style={styles.semImagemTexto}>{carregandoImagem ? 'Buscando imagem…' : 'Sem imagem desta câmera ainda'}</Text>
            </View>
          )}
          <Pressable
            style={styles.atualizar}
            onPress={() => ponto.cameraId && carregarSnapshot(ponto.cameraId, true)}
            disabled={carregandoImagem}
            accessibilityLabel="Atualizar imagem"
          >
            <RefreshCw size={16} color={colors.text} />
            <Text style={styles.atualizarTexto}>Atualizar imagem</Text>
          </Pressable>
        </View>
      ) : ponto ? (
        <View style={[styles.imagemWrap, styles.semImagem]}>
          <DoorOpen size={iconSize.xl} color={colors.mute} />
          <Text style={styles.semImagemTexto}>Nenhuma câmera ligada a este acesso. Edite o acesso para escolher uma.</Text>
        </View>
      ) : null}

      {snapshot?.capturadaEmUtc ? (
        <Text style={styles.legenda}>
          Imagem de {new Date(snapshot.capturadaEmUtc).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
        </Text>
      ) : null}

      {resultado ? (
        <View style={[styles.resultado, resultado.sucesso ? styles.resultadoOk : styles.resultadoErro]}>
          {resultado.sucesso ? <CheckCircle2 size={18} color={colors.safe} /> : <XCircle size={18} color={colors.danger} />}
          <Text style={styles.resultadoTexto}>{resultado.mensagem}</Text>
        </View>
      ) : null}

      {ponto && podeAbrir && ponto.podeAbrir ? (
        <View style={styles.acoes}>
          <PrimaryButton label="Abrir" icone={DoorOpen} onPress={abrir} loading={abrindo} />
          <PrimaryButton label="Agora não" variant="secondary" onPress={() => navigation.goBack()} />
        </View>
      ) : ponto ? (
        <Text style={styles.aviso}>
          {!podeAbrir
            ? 'Seu perfil não pode abrir portões nesta propriedade.'
            : 'Este acesso ainda não está ligado a nenhum equipamento que abra a porta.'}
        </Text>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tela: { flex: 1, backgroundColor: colors.bg },
  conteudo: { padding: spacing.xl, gap: spacing.md },
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
  subtitulo: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  imagemWrap: { borderRadius: radius.xl, overflow: 'hidden', backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line, minHeight: 220 },
  imagem: { width: '100%', aspectRatio: 16 / 9 },
  semImagem: { minHeight: 220, alignItems: 'center', justifyContent: 'center', gap: spacing.sm, padding: spacing.lg },
  semImagemTexto: { color: colors.mute, fontSize: fontSize.secondary, textAlign: 'center' },
  atualizar: {
    position: 'absolute',
    right: spacing.sm,
    bottom: spacing.sm,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    minHeight: 44,
    paddingHorizontal: spacing.md,
    borderRadius: radius.pill,
    backgroundColor: 'rgba(10,14,19,0.75)',
  },
  atualizarTexto: { color: colors.text, fontSize: fontSize.meta, fontWeight: fontWeight.bold },
  legenda: { color: colors.mute, fontSize: fontSize.meta },
  resultado: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, padding: spacing.md, borderRadius: radius.lg, borderWidth: 1 },
  resultadoOk: { backgroundColor: colors.marcaDim, borderColor: colors.marca },
  resultadoErro: { backgroundColor: colors.dangerDim, borderColor: colors.dangerLine },
  resultadoTexto: { color: colors.text, fontSize: fontSize.body, flex: 1 },
  acoes: { gap: spacing.sm, marginTop: spacing.sm },
  aviso: { color: colors.sub, fontSize: fontSize.secondary, textAlign: 'center', marginTop: spacing.sm },
  erro: { color: colors.danger, fontSize: fontSize.secondary },
});
