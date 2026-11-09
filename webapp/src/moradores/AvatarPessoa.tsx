import React from 'react';
import { Image, StyleSheet, Text, View } from 'react-native';
import type { MoradorResponse } from '../api/types';
import { colors, fontWeight } from '../theme/theme';
import { iniciais } from './rotulosPessoas';
import { useFotoMorador } from './useFotoMorador';

/** Foto de rosto salva no servidor, ou as iniciais quando ainda não há foto. */
export function AvatarPessoa({ pessoa, tamanho = 44, versao = 0 }: { pessoa: MoradorResponse; tamanho?: number; versao?: number }) {
  const uri = useFotoMorador(pessoa.id, pessoa.temFoto, versao);
  const forma = { width: tamanho, height: tamanho, borderRadius: tamanho / 2 };

  if (uri) {
    return <Image source={{ uri }} style={[styles.base, forma]} accessibilityLabel={`Foto de ${pessoa.nome}`} />;
  }
  return (
    <View style={[styles.base, styles.semFoto, forma]} accessibilityLabel={`${pessoa.nome}, sem foto`}>
      <Text style={[styles.iniciais, { fontSize: tamanho * 0.36 }]}>{iniciais(pessoa.nome)}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  base: { backgroundColor: colors.surface2, borderWidth: 1, borderColor: colors.line },
  semFoto: { alignItems: 'center', justifyContent: 'center' },
  iniciais: { color: colors.sub, fontWeight: fontWeight.bold },
});
