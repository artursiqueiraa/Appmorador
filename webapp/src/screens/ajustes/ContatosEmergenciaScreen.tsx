import React, { useCallback, useState } from 'react';
import { ActivityIndicator, Linking, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ArrowDown, ArrowUp, BellRing, ChevronLeft, Phone, Trash2, UserPlus } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import type { RootStackParamList } from '../../navigation/types';
import type { ContatoEmergenciaResponse } from '../../api/types';
import { ApiError } from '../../api/client';
import { formatarTelefone, servicoAlertas } from '../../alertas/servicoAlertas';
import { OpcoesChips } from '../../components/OpcoesChips';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

const LIMITE_CONTATOS = 5;
const OPCOES_MINUTOS = ['1', '2', '5', '10'] as const;

/**
 * Sprint 24 (ADR 0040) — quem é avisado quando um alerta fica sem resposta, depois de quanto
 * tempo, e o atalho para o alarme tocar mesmo no "Não perturbe". Só o dono altera; para quem não
 * é dono a API recusa e a tela explica.
 */
export function ContatosEmergenciaScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const propriedadeId = selectedProperty?.id ?? '';

  const [contatos, setContatos] = useState<ContatoEmergenciaResponse[] | null>(null);
  const [minutos, setMinutos] = useState<string | null>(null);
  const [semPermissao, setSemPermissao] = useState(false);
  const [nome, setNome] = useState('');
  const [telefone, setTelefone] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    if (!propriedadeId) {
      return;
    }
    try {
      const [lista, config] = await Promise.all([servicoAlertas.listarContatos(propriedadeId), servicoAlertas.obterConfiguracao(propriedadeId)]);
      setContatos(lista);
      setMinutos(String(config.minutosEscalonamento));
      setSemPermissao(false);
    } catch {
      setContatos([]);
      setSemPermissao(true);
    }
  }, [propriedadeId]);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar]),
  );

  const adicionar = async () => {
    setSalvando(true);
    setErro(null);
    try {
      const novo = await servicoAlertas.criarContato(propriedadeId, nome, telefone);
      setContatos((atual) => [...(atual ?? []), novo]);
      setNome('');
      setTelefone('');
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível salvar o contato.');
    } finally {
      setSalvando(false);
    }
  };

  const remover = async (contato: ContatoEmergenciaResponse) => {
    setContatos((atual) => atual?.filter((c) => c.id !== contato.id) ?? null);
    await servicoAlertas.removerContato(contato.id).catch(() => carregar());
  };

  const mover = async (indice: number, direcao: -1 | 1) => {
    if (!contatos) {
      return;
    }
    const destino = indice + direcao;
    const nova = contatos.map((c, i) => (i === indice ? contatos[destino] : i === destino ? contatos[indice] : c));
    setContatos(nova);
    await servicoAlertas.reordenarContatos(propriedadeId, nova.map((c) => c.id)).catch(() => carregar());
  };

  const alterarMinutos = async (valor: string) => {
    const anterior = minutos;
    setMinutos(valor);
    await servicoAlertas.salvarConfiguracao(propriedadeId, Number(valor)).catch(() => setMinutos(anterior));
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <Text style={styles.titulo}>Alertas e emergência</Text>
      </View>

      <Text style={styles.intro}>
        Quando o alarme dispara ou alguém aciona o pânico, você recebe um alerta. Se ninguém tocar em “Estou ciente”, avisamos as
        próximas pessoas — por último, seus contatos de emergência por SMS.
      </Text>

      {contatos === null ? (
        <ActivityIndicator color={colors.accent} style={{ marginTop: spacing.xl }} />
      ) : semPermissao ? (
        <View style={styles.aviso}>
          <Text style={styles.avisoTexto}>Só o responsável pela propriedade pode alterar os contatos de emergência.</Text>
        </View>
      ) : (
        <>
          <Text style={styles.secao}>Contatos de emergência</Text>
          {contatos.length === 0 ? (
            <Text style={styles.vazio}>Nenhum contato ainda. Adicione alguém de confiança que possa ir até o local ou ligar para você.</Text>
          ) : (
            <View style={styles.lista}>
              {contatos.map((c, i) => (
                <View key={c.id} style={styles.contato}>
                  <View style={styles.contatoIcone}>
                    <Phone size={iconSize.sm} color={colors.sub} />
                  </View>
                  <View style={styles.contatoTexto}>
                    <Text style={styles.contatoNome}>
                      {i + 1}. {c.nome}
                    </Text>
                    <Text style={styles.contatoTelefone}>{formatarTelefone(c.telefone)}</Text>
                  </View>
                  <Pressable onPress={() => mover(i, -1)} disabled={i === 0} style={[styles.acao, i === 0 && styles.acaoDesabilitada]} accessibilityLabel={`Subir ${c.nome}`}>
                    <ArrowUp size={16} color={colors.sub} />
                  </Pressable>
                  <Pressable
                    onPress={() => mover(i, 1)}
                    disabled={i === contatos.length - 1}
                    style={[styles.acao, i === contatos.length - 1 && styles.acaoDesabilitada]}
                    accessibilityLabel={`Descer ${c.nome}`}
                  >
                    <ArrowDown size={16} color={colors.sub} />
                  </Pressable>
                  <Pressable onPress={() => remover(c)} style={styles.acao} accessibilityLabel={`Remover ${c.nome}`}>
                    <Trash2 size={16} color={colors.danger} />
                  </Pressable>
                </View>
              ))}
            </View>
          )}

          {contatos.length < LIMITE_CONTATOS ? (
            <View style={styles.formulario}>
              <TextField label="Nome" value={nome} onChangeText={setNome} placeholder="Ex.: Ana (vizinha)" autoCapitalize="words" />
              <TextField label="Celular com DDD" value={telefone} onChangeText={setTelefone} placeholder="(11) 98765-4321" keyboardType="phone-pad" />
              {erro ? <Text style={styles.erro}>{erro}</Text> : null}
              <PrimaryButton label="Adicionar contato" icone={UserPlus} onPress={adicionar} loading={salvando} disabled={!nome.trim() || !telefone.trim()} />
            </View>
          ) : (
            <Text style={styles.vazio}>Você chegou ao limite de {LIMITE_CONTATOS} contatos.</Text>
          )}

          <Text style={styles.secao}>Tempo até avisar o próximo</Text>
          <OpcoesChips
            rotulo="Sem confirmação depois de"
            opcoes={OPCOES_MINUTOS.map((m) => ({ valor: m, rotulo: `${m} min` }))}
            valor={minutos}
            onChange={alterarMinutos}
            dica="Tempo curto avisa mais rápido; tempo longo dá mais chance de você mesmo ver primeiro."
          />
        </>
      )}

      {Platform.OS === 'android' ? (
        <>
          <Text style={styles.secao}>Tocar mesmo no “Não perturbe”</Text>
          <View style={styles.aviso}>
            <BellRing size={iconSize.md} color={colors.marca} />
            <Text style={styles.avisoTexto}>
              Para o alarme tocar com o celular no silencioso, abra os ajustes do app, entre em Notificações → “Alertas críticos” e
              permita ignorar o “Não perturbe”.
            </Text>
            <PrimaryButton label="Abrir ajustes do celular" variant="secondary" onPress={() => Linking.openSettings()} />
          </View>
        </>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.lg },
  iconBtn: {
    width: 36,
    height: 36,
    borderRadius: radius.sm,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  intro: { color: colors.sub, fontSize: fontSize.secondary, lineHeight: 19 },
  secao: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold, marginTop: spacing.xl, marginBottom: spacing.md },
  vazio: { color: colors.mute, fontSize: fontSize.secondary, marginBottom: spacing.md },
  lista: { gap: spacing.sm, marginBottom: spacing.lg },
  contato: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  contatoIcone: { width: 34, height: 34, borderRadius: radius.sm, backgroundColor: colors.surface2, alignItems: 'center', justifyContent: 'center' },
  contatoTexto: { flex: 1, minWidth: 0 },
  contatoNome: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  contatoTelefone: { color: colors.mute, fontSize: fontSize.meta, marginTop: 2 },
  acao: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  acaoDesabilitada: { opacity: 0.3 },
  formulario: { marginTop: spacing.sm },
  erro: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md },
  aviso: {
    gap: spacing.md,
    padding: spacing.lg,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginTop: spacing.md,
  },
  avisoTexto: { color: colors.sub, fontSize: fontSize.secondary, lineHeight: 19 },
});
