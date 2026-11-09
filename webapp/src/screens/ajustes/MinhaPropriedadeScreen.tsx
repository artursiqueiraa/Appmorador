import React, { useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Building2, ChevronRight, Cpu, Router, ShieldCheck, ShieldPlus, Users } from 'lucide-react-native';
import { api } from '../../api/client';
import type { DashboardResponse } from '../../api/types';
import { CartaoNeon } from '../../components/neon/CartaoNeon';
import { Degrade } from '../../components/neon/Degrade';
import { TelaCabecalho } from '../../components/neon/TelaCabecalho';
import { useAuth } from '../../auth/AuthContext';
import type { RootStackParamList } from '../../navigation/types';
import { colors, efeitos, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

/**
 * Sprint 16 (ADR 0019, UX001) — caminho permanente para configuração (nunca depende
 * de logout/login — corrige o bug do onboarding sumir). Exceção consciente e
 * documentada à Regra de Vocabulário: esta tela é a área técnica de instalação
 * (não o uso diário em Início/Câmeras/Acessos) — nomes de fabricante continuam
 * aparecendo aqui porque unificar o cadastro entre Control iD/JFL/Intelbras exigiria
 * mudança de backend, fora do escopo desta Sprint (ver ADR 0019 e DIVIDA_TECNICA).
 *
 * Sprint 17 (ADR 0020) — a seção "Proteção" leva a telas de gestão bruta de
 * fabricante (armar/desarmar partição, PGM, inibir zona por número) — exatamente o
 * que a auditoria da Sprint 17 (achado #5) identificou como "tela técnica". Fica
 * escondida para `perfil === 'morador'` (mesma preferência local de
 * `DetalhesEquipamentoScreen.tsx`), sem remover a funcionalidade para o técnico.
 */
export function MinhaPropriedadeScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty, perfil } = useAuth();
  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null);

  useEffect(() => {
    if (!selectedProperty) return;
    api.get<DashboardResponse>(`/api/properties/${selectedProperty.id}/dashboard`).then(setDashboard).catch(() => setDashboard(null));
  }, [selectedProperty]);

  if (!selectedProperty) {
    return null;
  }

  const propriedadeId = selectedProperty.id;
  const nomePropriedade = selectedProperty.nome;

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <TelaCabecalho titulo="Informações da Unidade" onVoltar={() => navigation.goBack()} semMarca />

      {/* Akteck Seg (ADR 0037) — cartão da propriedade e números reais (painel da propriedade). */}
      <CartaoNeon semPadding style={styles.cartaoPropriedade}>
        <View style={styles.banner}>
          <Degrade cores={efeitos.degradeFundo} />
          <View>
            <Building2 size={72} color={colors.marcaLine} strokeWidth={1.2} />
          </View>
        </View>
        <View style={styles.propriedadeTexto}>
          <Text style={styles.propriedadeNome}>{nomePropriedade}</Text>
          {selectedProperty.endereco ? <Text style={styles.subtitulo}>{selectedProperty.endereco}</Text> : null}
        </View>
      </CartaoNeon>

      <CartaoNeon semPadding>
        <LinhaNumero icon={Building2} label="Unidades" valor={dashboard ? String(dashboard.quantidadeUnidades) : '…'} onPress={() => navigation.navigate('Unidades', { propriedadeId, nomePropriedade })} />
        <LinhaNumero icon={Users} label="Pessoas cadastradas" valor={dashboard ? String(dashboard.quantidadePessoas) : '…'} onPress={() => navigation.navigate('MainTabs', { screen: 'Acessos' })} />
        <LinhaNumero
          icon={Cpu}
          label="Dispositivos online"
          valor={dashboard ? `${dashboard.quantidadeEquipamentosOnline} / ${dashboard.quantidadeEquipamentosOnline + dashboard.quantidadeEquipamentosOffline}` : '…'}
          ultimo
        />
      </CartaoNeon>

      <Text style={styles.secaoTitulo}>Configuração</Text>
      <MenuLinha
        icon={ShieldPlus}
        label="Continuar configuração guiada"
        onPress={() => navigation.navigate('Onboarding', { propriedadeId })}
      />

      <Text style={styles.secaoTitulo}>Estrutura</Text>
      <MenuLinha
        icon={Users}
        label="Unidades e moradores"
        onPress={() => navigation.navigate('Unidades', { propriedadeId, nomePropriedade })}
      />

      {perfil === 'tecnico' ? (
        <>
          <Text style={styles.secaoTitulo}>Proteção</Text>
          <MenuLinha
            icon={Router}
            label="Controladores de acesso"
            onPress={() => navigation.navigate('Equipamentos', { propriedadeId, nomePropriedade })}
          />
          <MenuLinha
            icon={ShieldCheck}
            label="Centrais JFL"
            onPress={() => navigation.navigate('CentraisJfl', { propriedadeId, nomePropriedade })}
          />
          <MenuLinha
            icon={ShieldPlus}
            label="Centrais Intelbras"
            onPress={() => navigation.navigate('CentraisIntelbras', { propriedadeId, nomePropriedade })}
          />
        </>
      ) : null}
    </ScrollView>
  );
}

function LinhaNumero({ icon: Icon, label, valor, onPress, ultimo }: { icon: typeof Users; label: string; valor: string; onPress?: () => void; ultimo?: boolean }) {
  return (
    <Pressable onPress={onPress} disabled={!onPress} style={[styles.linhaNumero, !ultimo && styles.linhaNumeroDivisor]} accessibilityRole={onPress ? 'button' : undefined}>
      <Icon size={iconSize.md} color={colors.marca} />
      <Text style={styles.linhaNumeroTexto}>{label}</Text>
      <Text style={styles.linhaNumeroValor}>{valor}</Text>
    </Pressable>
  );
}

function MenuLinha({ icon: Icon, label, onPress }: { icon: typeof Users; label: string; onPress: () => void }) {
  return (
    <Pressable style={styles.itemLinha} onPress={onPress}>
      <View style={styles.itemIconWrap}>
        <Icon size={iconSize.sm} color={colors.accent} />
      </View>
      <Text style={styles.itemTitulo}>{label}</Text>
      <View style={{ flex: 1 }} />
      <ChevronRight size={16} color={colors.mute} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  cartaoPropriedade: { overflow: 'hidden', marginBottom: spacing.md },
  banner: { height: 140, alignItems: 'center', justifyContent: 'center' },
  propriedadeTexto: { padding: spacing.lg, gap: spacing.xs },
  propriedadeNome: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  linhaNumero: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, minHeight: 56, paddingHorizontal: spacing.lg },
  linhaNumeroDivisor: { borderBottomWidth: 1, borderBottomColor: colors.lineSoft },
  linhaNumeroTexto: { flex: 1, color: colors.text, fontSize: fontSize.body },
  linhaNumeroValor: { color: colors.marca, fontSize: fontSize.section, fontWeight: fontWeight.black },
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.xl },
  iconBtn: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold },
  subtitulo: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  secaoTitulo: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, marginTop: spacing.lg, marginBottom: spacing.sm },
  itemLinha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
  },
  itemIconWrap: {
    width: 34,
    height: 34,
    borderRadius: radius.sm,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  itemTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium },
});
