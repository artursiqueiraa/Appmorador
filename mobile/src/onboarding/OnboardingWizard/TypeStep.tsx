import React, { useState } from 'react';
import { Text } from 'react-native';
import { Building2 } from 'lucide-react-native';
import { WizardStepLayout } from './WizardStepLayout';
import { TipoPropriedadeSelector, type TipoPropriedade } from '../../components/TipoPropriedadeSelector';
import { colors, fontSize } from '../../theme/theme';

interface Props {
  onAvancar: (tipo: TipoPropriedade) => void;
  salvando: boolean;
  erro: string | null;
  totalEtapas: number;
}

export function TypeStep({ onAvancar, salvando, erro, totalEtapas }: Props) {
  const [tipo, setTipo] = useState<TipoPropriedade | null>(null);

  return (
    <WizardStepLayout
      icon={Building2}
      titulo="Que tipo de propriedade é essa?"
      descricao="Isso ajuda a mostrar o que faz sentido pra você."
      etapaAtual={2}
      totalEtapas={totalEtapas}
      onAvancar={() => tipo && onAvancar(tipo)}
      avancarDesabilitado={!tipo}
      avancarCarregando={salvando}
    >
      <TipoPropriedadeSelector label="Tipo" value={tipo} onChange={setTipo} />
      {erro ? <Text style={{ color: colors.danger, fontSize: fontSize.secondary, marginTop: 8, textAlign: 'center' }}>{erro}</Text> : null}
    </WizardStepLayout>
  );
}
