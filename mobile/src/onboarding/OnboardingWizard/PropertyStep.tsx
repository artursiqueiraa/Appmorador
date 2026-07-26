import React, { useState } from 'react';
import { Home } from 'lucide-react-native';
import { WizardStepLayout } from './WizardStepLayout';
import { TextField } from '../../components/TextField';

interface Props {
  nome: string;
  endereco: string;
  onAvancar: (dados: { nome: string; endereco: string }) => void;
  totalEtapas: number;
}

export function PropertyStep({ nome: nomeInicial, endereco: enderecoInicial, onAvancar, totalEtapas }: Props) {
  const [nome, setNome] = useState(nomeInicial);
  const [endereco, setEndereco] = useState(enderecoInicial);

  return (
    <WizardStepLayout
      icon={Home}
      titulo="Como se chama sua propriedade?"
      descricao={'Um nome fácil de reconhecer, como "Minha casa" ou "Loja Centro".'}
      etapaAtual={1}
      totalEtapas={totalEtapas}
      onAvancar={() => onAvancar({ nome: nome.trim(), endereco: endereco.trim() })}
      avancarDesabilitado={!nome.trim()}
    >
      <TextField label="Nome" value={nome} onChangeText={setNome} placeholder="Ex.: Minha casa" />
      <TextField label="Endereço (opcional)" value={endereco} onChangeText={setEndereco} placeholder="Rua, número" />
    </WizardStepLayout>
  );
}
