import React, { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Switch, Text, View } from 'react-native';
import { ShieldCheck } from 'lucide-react-native';
import { api, ApiError } from '../api/client';
import { PrimaryButton } from '../components/PrimaryButton';
import { navigationRef } from '../navigation/navigationRef';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

export interface Consentimento {
  ativo: boolean;
  versaoTermo?: string | null;
  registradoEmUtc?: string | null;
  responsavelLegal: boolean;
  exigeResponsavel: boolean;
  versaoTermoAtual: string;
}

export const servicoConsentimento = {
  obter: (moradorId: string) => api.get<Consentimento>(`/api/moradores/${moradorId}/consentimento-biometrico`),
  registrar: (moradorId: string, responsavelLegal: boolean) =>
    api.post<Consentimento>(`/api/moradores/${moradorId}/consentimento-biometrico`, { responsavelLegal }),
  revogar: (moradorId: string) => api.delete<void>(`/api/moradores/${moradorId}/consentimento-biometrico`),
};

/**
 * Sprint 26 (ADR 0042, LGPD art. 11) — antes da câmera do cadastro facial: o termo de uso do
 * rosto. Sem "Concordo", a câmera não abre (e o servidor recusa a foto). Para dependente, quem
 * cadastra confirma ser o responsável legal.
 */
export function ConsentimentoFacial({ moradorId, nome, children }: { moradorId: string; nome: string; children: React.ReactNode }) {
  const [consentimento, setConsentimento] = useState<Consentimento | null>(null);
  const [responsavel, setResponsavel] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(() => {
    servicoConsentimento
      .obter(moradorId)
      .then(setConsentimento)
      .catch(() => setErro('Não foi possível carregar o consentimento.'));
  }, [moradorId]);

  useEffect(carregar, [carregar]);

  if (!consentimento) {
    return erro ? <Text style={styles.erro}>{erro}</Text> : <ActivityIndicator color={colors.marca} />;
  }

  if (consentimento.ativo) {
    return <>{children}</>;
  }

  const concordar = async () => {
    setEnviando(true);
    setErro(null);
    try {
      setConsentimento(await servicoConsentimento.registrar(moradorId, responsavel));
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível registrar o consentimento.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <View style={styles.cartao}>
      <View style={styles.cabecalho}>
        <ShieldCheck size={20} color={colors.marca} />
        <Text style={styles.titulo}>Antes de cadastrar o rosto</Text>
      </View>
      <Text style={styles.texto}>
        O rosto de {nome} é um dado pessoal sensível. Ele será usado só para liberar a entrada nos leitores faciais desta
        propriedade, guardado com segurança e apagado do servidor e dos leitores quando a pessoa ou a foto for removida, ou
        quando este consentimento for revogado.
      </Text>
      <Pressable onPress={() => navigationRef.navigate('Privacidade')} accessibilityRole="link">
        <Text style={styles.link}>Ler a Política de Privacidade</Text>
      </Pressable>
      {consentimento.exigeResponsavel ? (
        <View style={styles.linha}>
          <Switch
            value={responsavel}
            onValueChange={setResponsavel}
            trackColor={{ false: colors.surface2, true: colors.marcaDim }}
            thumbColor={responsavel ? colors.marca : colors.mute}
            accessibilityLabel="Sou o responsável legal"
          />
          <Text style={styles.textoLinha}>Sou o responsável legal por {nome} e autorizo o uso do rosto.</Text>
        </View>
      ) : null}
      {erro ? <Text style={styles.erro}>{erro}</Text> : null}
      <PrimaryButton
        label={consentimento.exigeResponsavel ? 'Concordo como responsável' : `${nome.split(' ')[0]} concorda`}
        onPress={concordar}
        loading={enviando}
        disabled={consentimento.exigeResponsavel && !responsavel}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  cartao: { gap: spacing.md, padding: spacing.lg, borderRadius: radius.lg, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line },
  cabecalho: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
  titulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  texto: { color: colors.sub, fontSize: fontSize.secondary, lineHeight: 19 },
  link: { color: colors.marca, fontSize: fontSize.secondary, fontWeight: fontWeight.bold },
  linha: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
  textoLinha: { color: colors.text, fontSize: fontSize.secondary, flex: 1 },
  erro: { color: colors.danger, fontSize: fontSize.secondary },
});
