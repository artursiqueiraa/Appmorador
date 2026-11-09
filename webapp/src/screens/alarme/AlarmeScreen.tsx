import React from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { CircleCheck, Settings, ShieldOff } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import { EstadoVazio } from '../../components/EstadoVazio';
import { OpcoesChips } from '../../components/OpcoesChips';
import { PrimaryButton } from '../../components/PrimaryButton';
import { SectionHeader } from '../../components/SectionHeader';
import { BotoesArme } from '../../alarme/BotoesArme';
import { podeOperar } from '../../pessoas/papeis';
import { HeroStatus } from '../../alarme/HeroStatus';
import { useCentralAlarme } from '../../alarme/useCentralAlarme';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

/**
 * Akteck Seg (ADR 0038) — aba "Alarme": o mesmo cartão de status e os mesmos três botões do
 * Início, mais as áreas (partições) da central. Comandos e status reais da central JFL.
 */
export function AlarmeScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const alarme = useCentralAlarme();

  if (!selectedProperty) return null;

  return (
    <ScrollView
      style={styles.tela}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={alarme.lendo} onRefresh={alarme.lerStatus} tintColor={colors.safe} />}
    >
      <View style={styles.cabecalho}>
        <View style={styles.cabecalhoTexto}>
          <Text style={styles.titulo}>Alarme</Text>
          <Text style={styles.subtitulo}>Arme, desarme e acompanhe cada área da casa</Text>
        </View>
        {alarme.centralId ? (
          <Pressable
            onPress={() => navigation.navigate('DetalhesCentralJfl', { equipamentoId: alarme.centralId! })}
            style={styles.iconBtn}
            accessibilityRole="button"
            accessibilityLabel="Configuração da central"
          >
            <Settings size={iconSize.md - 2} color={colors.sub} />
          </Pressable>
        ) : null}
      </View>

      {alarme.estado === 'semCentral' ? (
        <EstadoVazio
          icon={ShieldOff}
          titulo="Nenhuma central de alarme"
          descricao="Quando uma central for instalada nesta propriedade, você arma e desarma o alarme por aqui."
          cta={{ label: 'Configurar proteção', onPress: () => navigation.navigate('Onboarding', { propriedadeId: selectedProperty.id }) }}
        />
      ) : (
        <>
          {alarme.centrais.length > 1 ? (
            <OpcoesChips rotulo="Central" opcoes={alarme.centrais.map((c) => ({ valor: c.id, rotulo: c.nome }))} valor={alarme.centralId} onChange={alarme.selecionarCentral} />
          ) : null}
          <HeroStatus estado={alarme.estado} />
          <View style={styles.botoes}>
            <BotoesArme estado={alarme.estado} enviando={alarme.enviando} disponivel={alarme.particoes.length > 0 && podeOperar(selectedProperty?.perfil)} onComando={alarme.comandar} />
          </View>
          {alarme.erro ? <Text style={styles.erro}>{alarme.erro}</Text> : null}
          {alarme.estado === 'semComunicacao' && !alarme.lendo ? (
            <PrimaryButton label="Tentar falar com a central de novo" variant="secondary" onPress={alarme.lerStatus} />
          ) : null}

          {alarme.particoes.length > 0 ? (
            <>
              <SectionHeader title="Áreas monitoradas" />
              <View style={styles.areas}>
                {alarme.particoes.map((p) => {
                  const armada = p.armada || p.armadaStay;
                  const cor = p.emDisparo ? colors.danger : armada ? colors.safe : colors.warn;
                  return (
                    <View key={p.numero} style={styles.area}>
                      <CircleCheck size={iconSize.sm} color={cor} />
                      <Text style={styles.areaNome}>Área {p.numero}</Text>
                      <Text style={[styles.areaEstado, { color: cor }]}>{p.emDisparo ? 'Em disparo' : p.armadaStay ? 'Noturno' : armada ? 'Armada' : 'Desarmada'}</Text>
                    </View>
                  );
                })}
              </View>
            </>
          ) : null}
        </>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tela: { flex: 1, backgroundColor: colors.bg },
  content: { paddingHorizontal: 18, paddingBottom: spacing.xxl },
  cabecalho: { flexDirection: 'row', alignItems: 'center', paddingTop: spacing.sm, paddingBottom: spacing.lg, gap: spacing.md },
  cabecalhoTexto: { flex: 1, minWidth: 0 },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  subtitulo: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 3 },
  iconBtn: { width: 40, height: 40, borderRadius: radius.md, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line, alignItems: 'center', justifyContent: 'center' },
  botoes: { marginTop: spacing.lg },
  erro: { color: colors.danger, fontSize: fontSize.secondary, marginTop: spacing.sm, textAlign: 'center' },
  areas: { borderRadius: radius.lg, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line, paddingHorizontal: spacing.lg },
  area: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, minHeight: 48, borderBottomWidth: 1, borderBottomColor: colors.lineSoft },
  areaNome: { flex: 1, color: colors.text, fontSize: fontSize.body },
  areaEstado: { fontSize: fontSize.secondary, fontWeight: fontWeight.bold },
});
