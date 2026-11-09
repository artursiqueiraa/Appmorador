import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Car, Trash2 } from 'lucide-react-native';
import type { TipoVeiculo, VeiculoResponse } from '../api/types';
import { ApiError } from '../api/client';
import { Alert } from '../components/DialogoAlerta';
import { OpcoesChips } from '../components/OpcoesChips';
import { PrimaryButton } from '../components/PrimaryButton';
import { Skeleton } from '../components/Skeleton';
import { TextField } from '../components/TextField';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';
import { placaValida, TIPOS_VEICULO } from './rotulosPessoas';
import { servicoPessoas } from './servicoPessoas';

/** Formulário curto de veículo — usado aqui e na etapa 3 da criação guiada. */
export function FormularioVeiculo({ moradorId, onCriado, onCancelar }: { moradorId: string; onCriado: (v: VeiculoResponse) => void; onCancelar?: () => void }) {
  const [placa, setPlaca] = useState('');
  const [tipo, setTipo] = useState<TipoVeiculo>('Carro');
  const [modelo, setModelo] = useState('');
  const [cor, setCor] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const salvar = async () => {
    if (!placaValida(placa)) {
      setErro('Confira a placa: 3 letras e 4 caracteres, como ABC1D23 ou ABC1234.');
      return;
    }
    setSalvando(true);
    setErro(null);
    try {
      const criado = await servicoPessoas.criarVeiculo(moradorId, {
        placa: placa.replace(/[\s-]/g, '').toUpperCase(),
        tipo,
        modelo: modelo.trim() || undefined,
        cor: cor.trim() || undefined,
      });
      onCriado(criado);
      setPlaca('');
      setModelo('');
      setCor('');
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível salvar o veículo.');
    } finally {
      setSalvando(false);
    }
  };

  return (
    <View>
      <TextField label="Placa" value={placa} onChangeText={(v) => setPlaca(v.toUpperCase())} placeholder="ABC1D23" autoCapitalize="characters" maxLength={8} />
      <OpcoesChips rotulo="Tipo" opcoes={TIPOS_VEICULO} valor={tipo} onChange={setTipo} />
      <View style={styles.duasColunas}>
        <View style={styles.coluna}>
          <TextField label="Modelo (opcional)" value={modelo} onChangeText={setModelo} placeholder="Ex.: Onix" />
        </View>
        <View style={styles.coluna}>
          <TextField label="Cor (opcional)" value={cor} onChangeText={setCor} placeholder="Ex.: Prata" />
        </View>
      </View>
      {erro ? <Text style={styles.erro}>{erro}</Text> : null}
      <View style={styles.acoes}>
        <PrimaryButton label="Salvar veículo" onPress={salvar} loading={salvando} disabled={!placa.trim()} />
        {onCancelar ? <PrimaryButton label="Cancelar" variant="secondary" onPress={onCancelar} /> : null}
      </View>
    </View>
  );
}

/** Akteck Seg (ADR 0036) — veículos da pessoa, com cadastro na mesma tela (antes era outra tela, por morador). */
export function SecaoVeiculos({ moradorId, nome }: { moradorId: string; nome: string }) {
  const [veiculos, setVeiculos] = useState<VeiculoResponse[] | null>(null);
  const [erro, setErro] = useState(false);
  const [adicionando, setAdicionando] = useState(false);

  const carregar = useCallback(async () => {
    setErro(false);
    try {
      setVeiculos(await servicoPessoas.listarVeiculos(moradorId));
    } catch {
      setErro(true);
    }
  }, [moradorId]);

  useEffect(() => {
    setVeiculos(null);
    setAdicionando(false);
    carregar();
  }, [carregar]);

  const excluir = (v: VeiculoResponse) =>
    Alert.alert(`Remover ${v.placa}?`, `O veículo deixa de estar ligado a ${nome}.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          await servicoPessoas.excluirVeiculo(v.id).catch(() => undefined);
          carregar();
        },
      },
    ]);

  return (
    <View style={styles.secao}>
      <Text style={styles.titulo}>Veículos</Text>
      {veiculos === null && !erro ? (
        <Skeleton height={48} />
      ) : erro ? (
        <Text style={styles.erro}>Não foi possível carregar os veículos.</Text>
      ) : veiculos!.length === 0 && !adicionando ? (
        <Text style={styles.dica}>Nenhum veículo. Cadastre para liberar a entrada pela garagem.</Text>
      ) : (
        veiculos!.map((v) => (
          <View key={v.id} style={styles.veiculo}>
            <View style={styles.iconeWrap}>
              <Car size={iconSize.sm} color={colors.sub} />
            </View>
            <View style={styles.texto}>
              <Text style={styles.placa}>{v.placa}</Text>
              <Text style={styles.dica}>{[TIPOS_VEICULO.find((t) => t.valor === v.tipo)?.rotulo ?? v.tipo, v.modelo, v.cor].filter(Boolean).join(', ')}</Text>
            </View>
            <Pressable onPress={() => excluir(v)} style={styles.botaoIcone} accessibilityRole="button" accessibilityLabel={`Remover veículo ${v.placa}`}>
              <Trash2 size={iconSize.sm} color={colors.danger} />
            </Pressable>
          </View>
        ))
      )}
      {adicionando ? (
        <FormularioVeiculo
          moradorId={moradorId}
          onCriado={() => {
            setAdicionando(false);
            carregar();
          }}
          onCancelar={() => setAdicionando(false)}
        />
      ) : (
        <PrimaryButton label="Adicionar veículo" variant="secondary" onPress={() => setAdicionando(true)} />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  secao: { gap: spacing.md, padding: spacing.lg, borderRadius: radius.lg, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.line },
  titulo: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  veiculo: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
  iconeWrap: { width: 36, height: 36, borderRadius: radius.sm, backgroundColor: colors.surface2, alignItems: 'center', justifyContent: 'center' },
  texto: { flex: 1, minWidth: 0 },
  placa: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold, letterSpacing: 1 },
  dica: { color: colors.sub, fontSize: fontSize.secondary },
  erro: { color: colors.danger, fontSize: fontSize.secondary },
  botaoIcone: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  duasColunas: { flexDirection: 'row', gap: spacing.md, flexWrap: 'wrap' },
  coluna: { flex: 1, minWidth: 160 },
  acoes: { gap: spacing.sm },
});
