import React from 'react';
import { ShieldPlus } from 'lucide-react-native';
import { WizardStepLayout } from './WizardStepLayout';

interface Props {
  onConfigurarAgora: () => void;
  onPular: () => void;
  totalEtapas: number;
}

/** Opcional/pulável — a missão explicitamente proíbe forçar essa etapa. */
export function CentralStep({ onConfigurarAgora, onPular, totalEtapas }: Props) {
  return (
    <WizardStepLayout
      icon={ShieldPlus}
      titulo="Adicione uma central de alarme"
      descricao="Com uma central conectada, você pode armar, desarmar e acompanhar sua proteção em tempo real. Pode fazer isso agora ou depois, em Ajustes."
      etapaAtual={3}
      totalEtapas={totalEtapas}
      onAvancar={onConfigurarAgora}
      labelAvancar="Configurar agora"
      onPular={onPular}
    />
  );
}
