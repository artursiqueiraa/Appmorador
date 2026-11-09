import React from 'react';
import { Video } from 'lucide-react-native';
import { WizardStepLayout } from './WizardStepLayout';

interface Props {
  onVoltar?: () => void;
  onPular: () => void;
  totalEtapas: number;
}

/**
 * Opcional/pulável — hoje não existe nenhum cadastro de câmera real (sem API, ver
 * DIVIDA_TECNICA), então esta etapa só informa que o recurso está chegando, nunca
 * finge uma ação de "adicionar câmera" que não levaria a nada real.
 */
export function CameraStep({ onVoltar, onPular, totalEtapas }: Props) {
  return (
    <WizardStepLayout
      onVoltar={onVoltar}
      icon={Video}
      titulo="Câmeras estão chegando"
      descricao="Em breve você vai poder ver suas câmeras ao vivo direto pelo app. Continue a configuração — avisamos quando estiver disponível."
      etapaAtual={4}
      totalEtapas={totalEtapas}
      onAvancar={onPular}
      labelAvancar="Continuar"
    />
  );
}
