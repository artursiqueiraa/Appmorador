import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Fingerprint } from 'lucide-react-native';
import type { MoradorResponse } from '../api/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../theme/theme';
import { AvatarPessoa } from './AvatarPessoa';
import { descreverPapel } from './rotulosPessoas';

interface Props {
  pessoas: MoradorResponse[];
  selecionadaId?: string | null;
  onSelecionar: (p: MoradorResponse) => void;
  nomeUnidade?: string;
}

/** Akteck Seg (ADR 0038) — linhas de moradores do protótipo: foto, papel e selo "Facial" / "Pendente". */
export function ListaPessoas({ pessoas, selecionadaId, onSelecionar, nomeUnidade }: Props) {
  return (
    <View style={styles.container}>
      {pessoas.map((pessoa) => {
        const ativa = pessoa.id === selecionadaId;
        const papel = descreverPapel(pessoa);
        return (
          <Pressable
            key={pessoa.id}
            onPress={() => onSelecionar(pessoa)}
            style={(estado) => [styles.linha, (estado as { hovered?: boolean }).hovered && styles.linhaHover, ativa && styles.linhaAtiva]}
            accessibilityRole="button"
            accessibilityState={{ selected: ativa }}
            accessibilityLabel={`${pessoa.nome}, ${papel}${pessoa.temFoto ? ', rosto cadastrado' : ', sem rosto'}`}
          >
            <AvatarPessoa pessoa={pessoa} tamanho={42} />
            <View style={styles.texto}>
              <Text style={styles.nome} numberOfLines={1}>
                {pessoa.nome}
              </Text>
              <Text style={styles.papel} numberOfLines={1}>
                {nomeUnidade ? `${papel}, ${nomeUnidade}` : papel}
              </Text>
            </View>
            <View style={[styles.selo, pessoa.temFoto ? styles.seloOk : styles.seloPendente]}>
              {pessoa.temFoto ? <Fingerprint size={12} color={colors.safe} /> : null}
              <Text style={[styles.seloTexto, { color: pessoa.temFoto ? colors.safe : colors.warn }]}>{pessoa.temFoto ? 'Facial' : 'Pendente'}</Text>
            </View>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { gap: spacing.sm },
  linha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 66,
    paddingVertical: spacing.md,
    paddingHorizontal: 14,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  linhaHover: { backgroundColor: colors.surface2 },
  linhaAtiva: { borderColor: colors.safeLine, backgroundColor: colors.surface2 },
  texto: { flex: 1, minWidth: 0 },
  nome: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold },
  papel: { color: colors.mute, fontSize: fontSize.meta, marginTop: 2 },
  selo: { flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: 9, paddingVertical: 5, borderRadius: radius.sm },
  seloOk: { backgroundColor: colors.safeDim },
  seloPendente: { backgroundColor: colors.warnDim },
  seloTexto: { fontSize: fontSize.tiny, fontWeight: fontWeight.bold },
});
