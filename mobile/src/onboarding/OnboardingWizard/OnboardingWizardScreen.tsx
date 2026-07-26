import React, { useEffect, useState } from 'react';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useAuth } from '../../auth/AuthContext';
import { api, ApiError } from '../../api/client';
import type { PropriedadeResponse } from '../../api/types';
import type { TipoPropriedade } from '../../components/TipoPropriedadeSelector';
import type { RootStackParamList } from '../../navigation/types';
import { obterProgresso, salvarProgresso } from '../onboardingStorage';
import { WelcomeStep } from './WelcomeStep';
import { PropertyStep } from './PropertyStep';
import { TypeStep } from './TypeStep';
import { CentralStep } from './CentralStep';
import { CameraStep } from './CameraStep';
import { ResidentStep } from './ResidentStep';
import { FinishStep } from './FinishStep';

type OnboardingRouteProp = RouteProp<RootStackParamList, 'Onboarding'>;

const TOTAL_ETAPAS = 7;

/**
 * Sprint 16 (ADR 0019, UX001) — Wizard de configuração persistente. Progresso salvo
 * a partir do momento em que a Propriedade existe (etapas 0-2 acontecem antes disso,
 * então ficam só em memória — se o app fechar antes de criar a propriedade, o
 * usuário recomeça do Bem-vindo, o que é aceitável). Cada etapa pode ser pulada;
 * fechar o app em qualquer etapa 3+ e reabrir retoma exatamente onde parou —
 * corrige o bug "onboarding desaparece" registrado na missão desta Sprint.
 */
export function OnboardingWizardScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<OnboardingRouteProp>();
  const { selectProperty } = useAuth();

  const [etapa, setEtapa] = useState(params?.propriedadeId ? -1 : 0);
  const [propriedadeId, setPropriedadeId] = useState<string | undefined>(params?.propriedadeId);
  const [nome, setNome] = useState('');
  const [endereco, setEndereco] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    if (!params?.propriedadeId) {
      return;
    }

    obterProgresso(params.propriedadeId).then((progresso) => {
      setEtapa(Math.max(progresso.etapa, 3));
    });
  }, [params?.propriedadeId]);

  const irParaEtapa = async (proxima: number) => {
    setEtapa(proxima);
    if (propriedadeId) {
      await salvarProgresso(propriedadeId, { etapa: proxima, concluido: proxima >= TOTAL_ETAPAS - 1 });
    }
  };

  const criarPropriedade = async (tipo: TipoPropriedade) => {
    setSalvando(true);
    setErro(null);
    try {
      const criada = await api.post<PropriedadeResponse>('/api/properties', {
        nome,
        tipo,
        endereco: endereco || undefined,
      });
      setPropriedadeId(criada.id);
      await salvarProgresso(criada.id, { etapa: 3, concluido: false });
      selectProperty(criada);
      setEtapa(3);
    } catch (err) {
      setErro(err instanceof ApiError ? err.message : 'Não foi possível criar sua propriedade.');
    } finally {
      setSalvando(false);
    }
  };

  const concluir = async () => {
    if (propriedadeId) {
      await salvarProgresso(propriedadeId, { etapa: TOTAL_ETAPAS - 1, concluido: true });
    }
    navigation.reset({ index: 0, routes: [{ name: 'MainTabs' }] });
  };

  if (etapa === -1) {
    return null; // carregando o progresso salvo
  }

  if (etapa === 0) {
    return <WelcomeStep totalEtapas={TOTAL_ETAPAS} onAvancar={() => setEtapa(1)} />;
  }

  if (etapa === 1) {
    return (
      <PropertyStep
        nome={nome}
        endereco={endereco}
        totalEtapas={TOTAL_ETAPAS}
        onAvancar={(dados) => {
          setNome(dados.nome);
          setEndereco(dados.endereco);
          setEtapa(2);
        }}
      />
    );
  }

  if (etapa === 2) {
    return <TypeStep totalEtapas={TOTAL_ETAPAS} salvando={salvando} erro={erro} onAvancar={criarPropriedade} />;
  }

  if (etapa === 3) {
    return (
      <CentralStep
        totalEtapas={TOTAL_ETAPAS}
        onConfigurarAgora={() => navigation.navigate('MinhaPropriedade')}
        onPular={() => irParaEtapa(4)}
      />
    );
  }

  if (etapa === 4) {
    return <CameraStep totalEtapas={TOTAL_ETAPAS} onPular={() => irParaEtapa(5)} />;
  }

  if (etapa === 5) {
    return (
      <ResidentStep
        totalEtapas={TOTAL_ETAPAS}
        onConfigurarAgora={() => propriedadeId && navigation.navigate('Unidades', { propriedadeId, nomePropriedade: nome })}
        onPular={() => irParaEtapa(6)}
      />
    );
  }

  return <FinishStep totalEtapas={TOTAL_ETAPAS} onConcluir={concluir} />;
}
