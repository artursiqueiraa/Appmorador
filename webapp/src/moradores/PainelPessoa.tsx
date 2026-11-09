import React, { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ChevronRight, KeyRound, Pencil } from 'lucide-react-native';
import type { MoradorResponse, SalvarMoradorRequest } from '../api/types';
import { ApiError } from '../api/client';
import { Alert } from '../components/DialogoAlerta';
import { PrimaryButton } from '../components/PrimaryButton';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';
import { AvatarPessoa } from './AvatarPessoa';
import { FormularioPessoa } from './FormularioPessoa';
import { descreverPapel } from './rotulosPessoas';
import { SecaoRosto } from './SecaoRosto';
import { SecaoVeiculos } from './SecaoVeiculos';
import { servicoPessoas } from './servicoPessoas';

interface Props {
  pessoa: MoradorResponse;
  onAlterada: (p: MoradorResponse) => void;
  onRemovida: () => void;
  onAbrirCredenciais: () => void;
}

/**
 * Akteck Seg (ADR 0036) — tudo de uma pessoa num lugar só: dados, rosto e veículos. Antes
 * eram três telas separadas (Moradores → Credenciais / Veículos), cada uma a quatro toques
 * de distância do menu.
 */
export function PainelPessoa({ pessoa, onAlterada, onRemovida, onAbrirCredenciais }: Props) {
  const [editando, setEditando] = useState(false);
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [versaoFoto, setVersaoFoto] = useState(0);

  const salvar = async (dados: SalvarMoradorRequest) => {
    setSalvando(true);
    setErro(null);
    try {
      onAlterada(await servicoPessoas.atualizarPessoa(pessoa.id, dados));
      setEditando(false);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível salvar.');
    } finally {
      setSalvando(false);
    }
  };

  const remover = () =>
    Alert.alert(`Remover ${pessoa.nome}?`, 'A pessoa sai da casa e perde os acessos e os veículos cadastrados. O histórico continua guardado.', [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await servicoPessoas.excluirPessoa(pessoa.id);
            onRemovida();
          } catch {
            setErro('Não foi possível remover agora.');
          }
        },
      },
    ]);

  return (
    <View style={styles.container}>
      <View style={styles.cabecalho}>
        <AvatarPessoa pessoa={pessoa} tamanho={64} versao={versaoFoto} />
        <View style={styles.cabecalhoTexto}>
          <Text style={styles.nome} accessibilityRole="header">
            {pessoa.nome}
          </Text>
          <Text style={styles.papel}>{descreverPapel(pessoa)}</Text>
        </View>
        {!editando ? (
          <Pressable onPress={() => setEditando(true)} style={styles.botaoIcone} accessibilityRole="button" accessibilityLabel="Editar dados">
            <Pencil size={iconSize.sm} color={colors.sub} />
          </Pressable>
        ) : null}
      </View>

      {editando ? (
        <View style={styles.secao}>
          <FormularioPessoa key={pessoa.id} inicial={pessoa} salvando={salvando} erro={erro} onSalvar={salvar} onCancelar={() => setEditando(false)} />
        </View>
      ) : pessoa.telefone || pessoa.documento ? (
        <View style={styles.dados}>
          {pessoa.telefone ? <Text style={styles.dado}>Telefone: {pessoa.telefone}</Text> : null}
          {pessoa.documento ? <Text style={styles.dado}>Documento: {pessoa.documento}</Text> : null}
        </View>
      ) : null}

      <SecaoRosto
        pessoa={pessoa}
        versaoFoto={versaoFoto}
        onAtualizado={(temFoto) => {
          setVersaoFoto((v) => v + 1);
          onAlterada({ ...pessoa, temFoto });
        }}
      />

      <SecaoVeiculos moradorId={pessoa.id} nome={pessoa.nome} />

      <Pressable onPress={onAbrirCredenciais} style={styles.link} accessibilityRole="button">
        <KeyRound size={iconSize.sm} color={colors.sub} />
        <Text style={styles.linkTexto}>Tags, senhas e outros acessos</Text>
        <ChevronRight size={iconSize.sm} color={colors.mute} />
      </Pressable>

      {!editando && erro ? <Text style={styles.erro}>{erro}</Text> : null}
      <PrimaryButton label="Remover pessoa" variant="secondary" onPress={remover} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { gap: spacing.lg },
  cabecalho: { flexDirection: 'row', alignItems: 'center', gap: spacing.lg },
  cabecalhoTexto: { flex: 1, minWidth: 0 },
  nome: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold },
  papel: { color: colors.sub, fontSize: fontSize.body, marginTop: 2 },
  botaoIcone: {
    width: 44,
    height: 44,
    borderRadius: radius.sm,
    borderWidth: 1,
    borderColor: colors.line,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
  },
  secao: { padding: spacing.lg, borderRadius: radius.lg, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line },
  dados: { gap: spacing.xs },
  dado: { color: colors.sub, fontSize: fontSize.secondary },
  link: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 52,
    paddingHorizontal: spacing.lg,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  linkTexto: { flex: 1, color: colors.text, fontSize: fontSize.body },
  erro: { color: colors.danger, fontSize: fontSize.secondary },
});
