import React, { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import type { MoradorResponse } from '../api/types';
import { Alert } from '../components/DialogoAlerta';
import { PrimaryButton } from '../components/PrimaryButton';
import { CartaoNeon } from '../components/neon/CartaoNeon';
import { colors, fontSize, fontWeight, spacing } from '../theme/theme';
import { AvatarPessoa } from './AvatarPessoa';
import { CapturaFacial } from './CapturaFacial';
import { servicoPessoas } from './servicoPessoas';
import { ConsentimentoFacial, servicoConsentimento } from './ConsentimentoFacial';
import { SituacaoLeitores } from './SituacaoLeitores';

interface Props {
  pessoa: MoradorResponse;
  versaoFoto: number;
  onAtualizado: (temFoto: boolean) => void;
}

/**
 * Akteck Seg (ADR 0036/0037) — rosto da pessoa no detalhe. Sem foto, abre direto o enquadramento
 * do "Cadastro Facial"; com foto, mostra a foto e permite trocar ou remover. A foto vai para o
 * servidor e, desde a Sprint 26 (ADR 0042), aos leitores faciais da propriedade — sempre depois do
 * consentimento (LGPD art. 11).
 */
export function SecaoRosto({ pessoa, versaoFoto, onAtualizado }: Props) {
  const [trocando, setTrocando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const remover = () =>
    Alert.alert('Remover foto do rosto?', `${pessoa.nome} vai precisar cadastrar o rosto de novo para usar o acesso facial.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await servicoPessoas.removerRosto(pessoa.id);
            onAtualizado(false);
          } catch {
            setErro('Não foi possível remover a foto agora.');
          }
        },
      },
    ]);

  if (!pessoa.temFoto || trocando) {
    return (
      <View style={styles.secao}>
        <Text style={styles.titulo}>Cadastro facial</Text>
        <ConsentimentoFacial moradorId={pessoa.id} nome={pessoa.nome}>
          <CapturaFacial
            moradorId={pessoa.id}
            onSalvo={() => {
              setTrocando(false);
              onAtualizado(true);
            }}
          />
        </ConsentimentoFacial>
        {trocando ? <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setTrocando(false)} /> : null}
      </View>
    );
  }

  return (
    <CartaoNeon style={styles.cartao}>
      <Text style={styles.titulo}>Cadastro facial</Text>
      <View style={styles.linha}>
        <AvatarPessoa pessoa={pessoa} tamanho={88} versao={versaoFoto} />
        <View style={styles.texto}>
          <Text style={styles.status}>Rosto cadastrado</Text>
          <Text style={styles.dica}>A foto está salva com segurança. Troque se a pessoa mudou bastante (barba, óculos).</Text>
        </View>
      </View>
      <SituacaoLeitores moradorId={pessoa.id} versao={versaoFoto} />
      {erro ? <Text style={styles.erro}>{erro}</Text> : null}
      <PrimaryButton label="Trocar foto" variant="secondary" onPress={() => setTrocando(true)} />
      <PrimaryButton label="Remover foto" variant="danger" onPress={remover} />
      <PrimaryButton
        label="Revogar consentimento"
        variant="secondary"
        onPress={() =>
          Alert.alert('Revogar o consentimento?', `O rosto de ${pessoa.nome} será apagado do servidor e dos leitores.`, [
            { text: 'Cancelar', style: 'cancel' },
            {
              text: 'Revogar',
              style: 'destructive',
              onPress: async () => {
                try {
                  await servicoConsentimento.revogar(pessoa.id);
                  onAtualizado(false);
                } catch {
                  setErro('Não foi possível revogar agora.');
                }
              },
            },
          ])
        }
      />
    </CartaoNeon>
  );
}

const styles = StyleSheet.create({
  secao: { gap: spacing.sm },
  cartao: { gap: spacing.sm },
  titulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  linha: { flexDirection: 'row', alignItems: 'center', gap: spacing.lg, marginTop: spacing.sm },
  texto: { flex: 1, minWidth: 0, gap: spacing.xs },
  status: { color: colors.marca, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  dica: { color: colors.sub, fontSize: fontSize.secondary },
  erro: { color: colors.danger, fontSize: fontSize.secondary },
});
