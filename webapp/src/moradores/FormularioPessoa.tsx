import React, { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { IdCard, Link2, Phone, User } from 'lucide-react-native';
import type { MoradorResponse, Parentesco, SalvarMoradorRequest, TipoMorador } from '../api/types';
import { OpcoesChips } from '../components/OpcoesChips';
import { PrimaryButton } from '../components/PrimaryButton';
import { TextField } from '../components/TextField';
import { CartaoNeon } from '../components/neon/CartaoNeon';
import { colors, fontSize, fontWeight, iconSize, spacing } from '../theme/theme';
import { PARENTESCOS } from './rotulosPessoas';
import { SeletorVinculo, type Vinculo } from './SeletorVinculo';

interface Props {
  inicial?: MoradorResponse;
  /** Na criação guiada o botão é do assistente; aqui só os campos. */
  semBotoes?: boolean;
  salvando?: boolean;
  erro?: string | null;
  onAlterar?: (dados: SalvarMoradorRequest, valido: boolean) => void;
  onSalvar?: (dados: SalvarMoradorRequest) => void;
  onCancelar?: () => void;
  /** Quando informado, o vínculo "Visitante" aparece e chama isto (cadastro de visitantes já existente). */
  onVisitante?: () => void;
}

function TituloCartao({ icone: Icone, texto, obrigatorio }: { icone: typeof User; texto: string; obrigatorio?: boolean }) {
  return (
    <View style={styles.tituloCartao}>
      <Icone size={iconSize.md} color={colors.marca} />
      <Text style={styles.tituloCartaoTexto}>
        {texto}
        {obrigatorio ? <Text style={styles.obrigatorio}> *</Text> : null}
      </Text>
    </View>
  );
}

/** Akteck Seg (ADR 0036/0037) — dados de uma pessoa da casa, no layout "Cadastrar Pessoa" da referência. */
export function FormularioPessoa({ inicial, semBotoes, salvando, erro, onAlterar, onSalvar, onCancelar, onVisitante }: Props) {
  const [nome, setNome] = useState(inicial?.nome ?? '');
  const [tipo, setTipo] = useState<TipoMorador>(inicial?.tipo ?? 'Titular');
  const [parentesco, setParentesco] = useState<Parentesco | null>(inicial?.parentesco ?? null);
  const [telefone, setTelefone] = useState(inicial?.telefone ?? '');
  const [documento, setDocumento] = useState(inicial?.documento ?? '');

  const montar = (parcial: Partial<{ nome: string; tipo: TipoMorador; parentesco: Parentesco | null; telefone: string; documento: string }>) => {
    const d = { nome, tipo, parentesco, telefone, documento, ...parcial };
    const dados: SalvarMoradorRequest = {
      nome: d.nome.trim(),
      tipo: d.tipo,
      parentesco: d.tipo === 'Dependente' ? d.parentesco : null,
      telefone: d.telefone.trim() || undefined,
      documento: d.documento.trim() || undefined,
    };
    onAlterar?.(dados, dados.nome.length > 0);
    return dados;
  };

  const escolherVinculo = (v: Vinculo) => {
    if (v === 'Visitante') {
      onVisitante?.();
      return;
    }
    setTipo(v);
    montar({ tipo: v });
  };

  return (
    <View style={styles.container}>
      <CartaoNeon>
        <TituloCartao icone={Link2} texto="Tipo de vínculo" obrigatorio />
        <SeletorVinculo valor={tipo} onChange={escolherVinculo} semVisitante={!onVisitante} />
        {tipo === 'Dependente' ? (
          <View style={styles.parentesco}>
            <OpcoesChips
              rotulo="Parentesco (opcional)"
              opcoes={PARENTESCOS}
              valor={parentesco}
              onChange={(v) => {
                setParentesco(v);
                montar({ parentesco: v });
              }}
            />
          </View>
        ) : null}
      </CartaoNeon>

      <CartaoNeon>
        <TituloCartao icone={User} texto="Dados pessoais" />
        <TextField
          label="Nome completo *"
          icone={User}
          value={nome}
          onChangeText={(v) => {
            setNome(v);
            montar({ nome: v });
          }}
          placeholder="Ex.: João Silva"
          autoFocus={!inicial}
        />
        <TextField
          label="CPF"
          icone={IdCard}
          value={documento}
          onChangeText={(v) => {
            setDocumento(v);
            montar({ documento: v });
          }}
          placeholder="000.000.000-00"
          keyboardType="numeric"
        />
        <TextField
          label="Telefone"
          icone={Phone}
          value={telefone}
          onChangeText={(v) => {
            setTelefone(v);
            montar({ telefone: v });
          }}
          keyboardType="phone-pad"
          placeholder="(27) 9 9999-9999"
        />
      </CartaoNeon>

      {erro ? <Text style={styles.erro}>{erro}</Text> : null}
      {semBotoes ? null : (
        <View style={styles.acoes}>
          <PrimaryButton label={inicial ? 'Salvar alterações' : 'Adicionar pessoa'} onPress={() => onSalvar?.(montar({}))} loading={salvando} disabled={!nome.trim()} />
          {onCancelar ? <PrimaryButton label="Cancelar" variant="secondary" onPress={onCancelar} /> : null}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { width: '100%', gap: spacing.md },
  tituloCartao: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, marginBottom: spacing.lg },
  tituloCartaoTexto: { color: colors.text, fontSize: fontSize.section, fontWeight: fontWeight.bold },
  obrigatorio: { color: colors.danger },
  parentesco: { marginTop: spacing.lg },
  acoes: { gap: spacing.sm },
  erro: { color: colors.danger, fontSize: fontSize.secondary },
});
