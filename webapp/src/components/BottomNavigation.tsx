import React, { useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, Text, View, type PressableStateCallbackType } from 'react-native';
import { BlurView } from 'expo-blur';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import type { BottomTabBarProps } from '@react-navigation/bottom-tabs';
import { Home, Menu, Shield, Users, Video, X } from 'lucide-react-native';
import { useAuth } from '../auth/AuthContext';
import { ITENS_MENU, type ItemMenu } from '../navigation/itensMenu';
import { colors, fontSize, fontWeight, iconSize, layout, radius, spacing } from '../theme/theme';
import { MarcaAkteck } from './marca/MarcaAkteck';

const ICONES: Record<string, typeof Home> = {
  Inicio: Home,
  Cameras: Video,
  Alarme: Shield,
  Acessos: Users,
};

const ROTULOS: Record<string, string> = {
  Inicio: 'Início',
  Cameras: 'Câmeras',
  Alarme: 'Alarme',
  Acessos: 'Moradores',
};

type Estado = PressableStateCallbackType & { hovered?: boolean };

/** Itens do menu agrupados (Casa / Conta). Usado no menu lateral do celular e na barra do computador. */
function ListaMenu({ onEscolher }: { onEscolher: (item: ItemMenu) => void }) {
  const { selectedProperty } = useAuth();
  return (
    <>
      {(['Casa', 'Conta'] as const).map((grupo) => (
        <View key={grupo} style={styles.grupo}>
          <Text style={styles.grupoTitulo}>{grupo}</Text>
          {ITENS_MENU.filter((i) => i.grupo === grupo && (!i.visivel || (selectedProperty ? i.visivel(selectedProperty) : false))).map((item) => (
            <Pressable key={item.id} onPress={() => onEscolher(item)} style={(e: Estado) => [styles.itemMenu, e.hovered && styles.itemHover]} accessibilityRole="button">
              <item.icone size={iconSize.md - 2} color={colors.sub} />
              <Text style={styles.itemMenuTexto}>{item.rotulo}</Text>
            </Pressable>
          ))}
        </View>
      ))}
    </>
  );
}

/**
 * Sprint 16 (ADR 0019) — navegação inferior fixa. Akteck Seg (ADR 0038): as 4 abas do dia a dia
 * (Início, Câmeras, Alarme, Moradores) + "Menu", que abre um menu lateral com o resto. No
 * computador tudo fica numa barra lateral fixa: abas em cima, menu completo embaixo.
 */
export function BottomNavigation({ state, navigation, lateral = false }: BottomTabBarProps & { lateral?: boolean }) {
  const insets = useSafeAreaInsets();
  const { selectedProperty, trocarPropriedade } = useAuth();
  const [menuAberto, setMenuAberto] = useState(false);

  const escolher = (item: ItemMenu) => {
    setMenuAberto(false);
    if (selectedProperty) item.abrir(selectedProperty, { trocarPropriedade });
  };

  const abas = state.routes.map((route, index) => {
    const ativo = state.index === index;
    const Icone = ICONES[route.name] ?? Home;
    const rotulo = ROTULOS[route.name] ?? route.name;
    return { route, ativo, Icone, rotulo };
  });

  if (lateral) {
    return (
      <View style={styles.lateral}>
        <View style={styles.marca}>
          <MarcaAkteck tamanho={24} />
        </View>
        {selectedProperty ? (
          <Pressable onPress={trocarPropriedade} style={styles.propriedade} accessibilityRole="button" accessibilityLabel={`${selectedProperty.nome}. Trocar de propriedade`}>
            <Text style={styles.propriedadeRotulo}>Propriedade</Text>
            <Text style={styles.propriedadeNome} numberOfLines={1}>
              {selectedProperty.nome}
            </Text>
          </Pressable>
        ) : null}
        <ScrollView contentContainerStyle={styles.lateralConteudo}>
          <View accessibilityRole="tablist" style={styles.grupo}>
            {abas.map(({ route, ativo, Icone, rotulo }) => (
              <Pressable
                key={route.key}
                onPress={() => navigation.navigate(route.name)}
                style={(e: Estado) => [styles.itemMenu, e.hovered && styles.itemHover, ativo && styles.itemAtivo]}
                accessibilityRole="tab"
                accessibilityState={{ selected: ativo }}
                accessibilityLabel={rotulo}
              >
                <Icone size={iconSize.md} color={ativo ? colors.safe : colors.sub} />
                <Text style={[styles.itemMenuTexto, ativo && styles.itemTextoAtivo]}>{rotulo}</Text>
              </Pressable>
            ))}
          </View>
          <ListaMenu onEscolher={escolher} />
        </ScrollView>
      </View>
    );
  }

  return (
    <>
      <BlurView intensity={40} tint="dark" style={[styles.barra, { paddingBottom: Math.max(insets.bottom, spacing.sm) }]}>
        {abas.map(({ route, ativo, Icone, rotulo }) => (
          <Pressable
            key={route.key}
            onPress={() => navigation.navigate(route.name)}
            style={styles.item}
            accessibilityRole="tab"
            accessibilityState={{ selected: ativo }}
            accessibilityLabel={rotulo}
          >
            <Icone size={21} color={ativo ? colors.safe : colors.mute} />
            <Text style={[styles.label, ativo && styles.labelAtivo]}>{rotulo}</Text>
          </Pressable>
        ))}
        <Pressable onPress={() => setMenuAberto(true)} style={styles.item} accessibilityRole="button" accessibilityLabel="Menu">
          <Menu size={21} color={colors.mute} />
          <Text style={styles.label}>Menu</Text>
        </Pressable>
      </BlurView>

      <Modal visible={menuAberto} transparent animationType="fade" onRequestClose={() => setMenuAberto(false)}>
        <View style={styles.fundoMenu}>
          <View style={styles.gaveta} accessibilityViewIsModal>
            <View style={styles.gavetaTopo}>
              <MarcaAkteck tamanho={22} />
              <Pressable onPress={() => setMenuAberto(false)} style={styles.fechar} accessibilityRole="button" accessibilityLabel="Fechar menu">
                <X size={iconSize.md} color={colors.sub} />
              </Pressable>
            </View>
            {selectedProperty ? (
              <View style={styles.propriedade}>
                <Text style={styles.propriedadeRotulo}>Propriedade</Text>
                <Text style={styles.propriedadeNome} numberOfLines={1}>
                  {selectedProperty.nome}
                </Text>
              </View>
            ) : null}
            <ScrollView contentContainerStyle={styles.lateralConteudo}>
              <ListaMenu onEscolher={escolher} />
            </ScrollView>
          </View>
          <Pressable style={styles.fora} onPress={() => setMenuAberto(false)} accessibilityLabel="Fechar menu" />
        </View>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  barra: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingTop: spacing.sm,
    backgroundColor: 'rgba(10,14,19,0.85)',
    borderTopWidth: 1,
    borderTopColor: colors.lineSoft,
    overflow: 'hidden',
  },
  item: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: 4, minHeight: 52 },
  label: { fontSize: fontSize.label + 0.5, fontWeight: fontWeight.medium, color: colors.mute },
  labelAtivo: { color: colors.text },
  fundoMenu: { flex: 1, flexDirection: 'row', backgroundColor: colors.overlay },
  gaveta: { width: 300, maxWidth: '85%', height: '100%', backgroundColor: colors.bg2, borderRightWidth: 1, borderRightColor: colors.line },
  fora: { flex: 1 },
  gavetaTopo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingHorizontal: spacing.lg, paddingTop: spacing.xl, paddingBottom: spacing.md },
  fechar: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  lateral: { width: layout.sideNavWidth, backgroundColor: colors.bg2, borderRightWidth: 1, borderRightColor: colors.lineSoft, paddingTop: spacing.xl },
  marca: { paddingHorizontal: spacing.lg, marginBottom: spacing.lg },
  propriedade: {
    marginHorizontal: spacing.md,
    marginBottom: spacing.md,
    padding: spacing.md,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  propriedadeRotulo: { color: colors.mute, fontSize: fontSize.tiny },
  propriedadeNome: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold, marginTop: 2 },
  lateralConteudo: { paddingHorizontal: spacing.md, paddingBottom: spacing.xl, gap: spacing.lg },
  grupo: { gap: 2 },
  grupoTitulo: { color: colors.mute, fontSize: fontSize.tiny, fontWeight: fontWeight.medium, paddingHorizontal: spacing.md, marginBottom: spacing.xs },
  itemMenu: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, minHeight: 44, paddingHorizontal: spacing.md, borderRadius: radius.md },
  itemHover: { backgroundColor: colors.surface },
  itemAtivo: { backgroundColor: colors.safeDim },
  itemMenuTexto: { color: colors.sub, fontSize: fontSize.body, fontWeight: fontWeight.medium },
  itemTextoAtivo: { color: colors.text, fontWeight: fontWeight.bold },
});
