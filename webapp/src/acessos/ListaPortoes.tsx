import React, { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ChevronRight, DoorOpen, Video } from 'lucide-react-native';
import { api } from '../api/client';
import type { PontoAcessoResponse } from '../api/types';
import { navigationRef } from '../navigation/navigationRef';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';

/**
 * Sprint 24.5 — portões e portas que abrem pelo app (pontos de acesso com acionador), no Início.
 * Tocar abre a tela do acesso (câmera + Abrir/Agora não); nunca abre direto daqui.
 */
export function ListaPortoes({ propriedadeId, aoCarregar }: { propriedadeId: string; aoCarregar?: (quantidade: number) => void }) {
  const [portoes, setPortoes] = useState<PontoAcessoResponse[]>([]);

  useEffect(() => {
    let cancelado = false;
    api
      .get<PontoAcessoResponse[]>(`/api/properties/${propriedadeId}/pontos-acesso`)
      .then((lista) => {
        if (!cancelado) {
          const comAcionador = lista.filter((p) => p.podeAbrir);
          setPortoes(comAcionador);
          aoCarregar?.(comAcionador.length);
        }
      })
      .catch(() => aoCarregar?.(0));
    return () => {
      cancelado = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [propriedadeId]);

  if (portoes.length === 0) {
    return null;
  }

  return (
    <View style={styles.lista}>
      {portoes.map((p) => (
        <Pressable
          key={p.id}
          style={styles.item}
          onPress={() => navigationRef.navigate('AcessoPorta', { pontoAcessoId: p.id, propriedadeId, nome: p.nome })}
          accessibilityRole="button"
          accessibilityLabel={`${p.nome}. Ver ${p.cameraId ? 'câmera' : 'acesso'} e abrir`}
        >
          <View style={styles.icone}>
            <DoorOpen size={18} color={p.acionadorOnline ? colors.safe : colors.mute} />
          </View>
          <View style={styles.texto}>
            <Text style={styles.nome} numberOfLines={1}>
              {p.nome}
            </Text>
            <Text style={styles.detalhe} numberOfLines={1}>
              {p.acionadorOnline ? 'Toque para ver e abrir' : 'Equipamento sem comunicação'}
            </Text>
          </View>
          {p.cameraId ? <Video size={16} color={colors.sub} /> : null}
          <ChevronRight size={18} color={colors.mute} />
        </Pressable>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  lista: { gap: spacing.sm },
  item: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 60,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  icone: { width: 36, height: 36, borderRadius: radius.md, backgroundColor: colors.surface2, alignItems: 'center', justifyContent: 'center' },
  texto: { flex: 1, minWidth: 0 },
  nome: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  detalhe: { color: colors.mute, fontSize: fontSize.meta, marginTop: 2 },
});
