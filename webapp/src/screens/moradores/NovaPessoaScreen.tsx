import React, { useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ArrowRight, Car, CircleCheck, ScanFace, UserPlus } from 'lucide-react-native';
import { ApiError } from '../../api/client';
import type { MoradorResponse, SalvarMoradorRequest } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { PrimaryButton } from '../../components/PrimaryButton';
import { CartaoNeon } from '../../components/neon/CartaoNeon';
import { TelaCabecalho } from '../../components/neon/TelaCabecalho';
import { AvatarPessoa } from '../../moradores/AvatarPessoa';
import { CapturaFacial } from '../../moradores/CapturaFacial';
import { FormularioPessoa } from '../../moradores/FormularioPessoa';
import { FormularioVeiculo } from '../../moradores/SecaoVeiculos';
import { servicoPessoas } from '../../moradores/servicoPessoas';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, spacing } from '../../theme/theme';

const TOTAL = 4;

/**
 * Akteck Seg (ADR 0036/0037) — cadastro guiado no layout da referência: "Cadastrar Pessoa"
 * (vínculo + dados) → "Cadastro Facial" → veículo → pronto. A pessoa é criada ao sair da etapa 1;
 * rosto e veículo podem ser pulados e feitos depois, no detalhe da pessoa.
 */
export function NovaPessoaScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<RouteProp<RootStackParamList, 'NovaPessoa'>>();
  const { selectedProperty } = useAuth();
  const [etapa, setEtapa] = useState(0);
  const [dados, setDados] = useState<SalvarMoradorRequest | null>(null);
  const [valido, setValido] = useState(false);
  const [pessoa, setPessoa] = useState<MoradorResponse | null>(null);
  const [ocupado, setOcupado] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [veiculos, setVeiculos] = useState<string[]>([]);

  const criar = async () => {
    if (!dados || !valido) return;
    setOcupado(true);
    setErro(null);
    try {
      setPessoa(await servicoPessoas.criarPessoa(params.unidadeId, dados));
      setEtapa(1);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível adicionar a pessoa.');
    } finally {
      setOcupado(false);
    }
  };

  const primeiroNome = (pessoa?.nome ?? dados?.nome ?? '').split(' ')[0];
  const progresso = <Text style={styles.etapa}>Etapa {etapa + 1} de {TOTAL}</Text>;

  const cabecalho =
    etapa === 0 ? (
      <TelaCabecalho icone={UserPlus} titulo="Cadastrar Pessoa" subtitulo="Cadastre moradores, dependentes ou visitantes." onVoltar={() => navigation.goBack()} />
    ) : etapa === 1 ? (
      <TelaCabecalho
        icone={ScanFace}
        titulo="Cadastro Facial"
        subtitulo={`Capture o rosto de ${primeiroNome} para a liberação de acesso com reconhecimento facial.`}
        onVoltar={() => navigation.goBack()}
      />
    ) : etapa === 2 ? (
      <TelaCabecalho icone={Car} titulo={`Veículos de ${primeiroNome}`} subtitulo="Cadastre a placa para liberar a entrada pela garagem." onVoltar={() => setEtapa(1)} />
    ) : (
      <TelaCabecalho icone={CircleCheck} titulo="Cadastro concluído" subtitulo={`${primeiroNome} já aparece em Pessoas.`} />
    );

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      {cabecalho}
      {progresso}

      {etapa === 0 ? (
        <>
          <FormularioPessoa
            semBotoes
            erro={erro}
            onAlterar={(d, ok) => {
              setDados(d);
              setValido(ok);
            }}
            onVisitante={
              selectedProperty
                ? () => navigation.replace('Visitantes', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome })
                : undefined
            }
          />
          <PrimaryButton label="Continuar" icone={ArrowRight} onPress={criar} loading={ocupado} disabled={!valido} />
        </>
      ) : null}

      {etapa === 1 && pessoa ? (
        <>
          <CapturaFacial
            moradorId={pessoa.id}
            onSalvo={() => {
              setPessoa({ ...pessoa, temFoto: true });
              setEtapa(2);
            }}
          />
          <PrimaryButton label="Pular por agora" variant="secondary" onPress={() => setEtapa(2)} />
        </>
      ) : null}

      {etapa === 2 && pessoa ? (
        <>
          {veiculos.length > 0 ? <Text style={styles.sucesso}>Cadastrado: {veiculos.join(', ')}</Text> : null}
          <CartaoNeon>
            <FormularioVeiculo moradorId={pessoa.id} onCriado={(v) => setVeiculos((atual) => [...atual, v.placa])} />
          </CartaoNeon>
          <PrimaryButton label={veiculos.length > 0 ? 'Continuar' : 'Não tem veículo'} variant={veiculos.length > 0 ? 'primary' : 'secondary'} onPress={() => setEtapa(3)} />
        </>
      ) : null}

      {etapa === 3 && pessoa ? (
        <CartaoNeon style={styles.pronto}>
          <AvatarPessoa pessoa={pessoa} tamanho={96} />
          <Text style={styles.prontoNome}>{pessoa.nome}</Text>
          <Text style={styles.prontoTexto}>
            {pessoa.temFoto ? 'Rosto cadastrado.' : 'Falta o rosto: abra a pessoa em Pessoas e toque em "Capturar Foto".'}
            {veiculos.length > 0 ? ` ${veiculos.length === 1 ? 'Veículo cadastrado' : 'Veículos cadastrados'}: ${veiculos.join(', ')}.` : ''}
          </Text>
          <View style={styles.prontoBotao}>
            <PrimaryButton label="Voltar para Pessoas" onPress={() => navigation.goBack()} />
          </View>
        </CartaoNeon>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl, gap: spacing.md },
  etapa: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  sucesso: { color: colors.marca, fontSize: fontSize.secondary },
  pronto: { alignItems: 'center', gap: spacing.sm, paddingVertical: spacing.xxl },
  prontoNome: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black, marginTop: spacing.sm },
  prontoTexto: { color: colors.sub, fontSize: fontSize.body, textAlign: 'center' },
  prontoBotao: { alignSelf: 'stretch', marginTop: spacing.md },
});
