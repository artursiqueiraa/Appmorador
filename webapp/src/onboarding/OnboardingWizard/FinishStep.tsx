import React from 'react';
import { PartyPopper } from 'lucide-react-native';
import { WizardStepLayout } from './WizardStepLayout';

interface Props {
  onVoltar?: () => void;
  onConcluir: () => void;
  totalEtapas: number;
}

export function FinishStep({ onVoltar, onConcluir, totalEtapas }: Props) {
  return (
    <WizardStepLayout
      onVoltar={onVoltar}
      icon={PartyPopper}
      titulo="Tudo pronto!"
      descricao="Sua propriedade está configurada. Você pode continuar a configuração quando quiser em Ajustes → Minha Propriedade."
      etapaAtual={6}
      totalEtapas={totalEtapas}
      onAvancar={onConcluir}
      labelAvancar="Ir para o início"
    />
  );
}
