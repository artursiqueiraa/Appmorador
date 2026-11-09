import React from 'react';
import { ShieldCheck } from 'lucide-react-native';
import { WizardStepLayout } from './WizardStepLayout';

interface Props {
  onVoltar?: () => void;
  onAvancar: () => void;
  totalEtapas: number;
}

export function WelcomeStep({ onVoltar, onAvancar, totalEtapas }: Props) {
  return (
    <WizardStepLayout
      onVoltar={onVoltar}
      icon={ShieldCheck}
      titulo="Vamos proteger sua casa"
      descricao="Em poucos passos você configura tudo. Cada etapa pode ser pulada e retomada depois, quando quiser."
      etapaAtual={0}
      totalEtapas={totalEtapas}
      onAvancar={onAvancar}
      labelAvancar="Começar"
    />
  );
}
