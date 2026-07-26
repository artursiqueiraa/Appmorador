import React from 'react';
import { Users } from 'lucide-react-native';
import { WizardStepLayout } from './WizardStepLayout';

interface Props {
  onConfigurarAgora: () => void;
  onPular: () => void;
  totalEtapas: number;
}

/** Opcional/pulável. */
export function ResidentStep({ onConfigurarAgora, onPular, totalEtapas }: Props) {
  return (
    <WizardStepLayout
      icon={Users}
      titulo="Quem mais mora com você?"
      descricao="Cadastre as pessoas da sua casa para liberar acesso e credenciais para elas também."
      etapaAtual={5}
      totalEtapas={totalEtapas}
      onAvancar={onConfigurarAgora}
      labelAvancar="Adicionar agora"
      onPular={onPular}
    />
  );
}
