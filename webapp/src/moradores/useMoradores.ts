import { useCallback, useEffect, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import type { MoradorResponse, UnidadeResponse } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { unidadePadrao } from './rotulosPessoas';
import { servicoPessoas } from './servicoPessoas';

/**
 * Akteck Seg (ADR 0036) — estado da aba Moradores. "Unidade" é um conceito do sistema que o
 * morador comum não precisa conhecer: numa casa com uma unidade só, ela nem aparece. Se a
 * propriedade ainda não tem nenhuma, uma unidade "Principal" é criada só na hora de adicionar
 * a primeira pessoa (nunca só por abrir a tela).
 */
export function useMoradores() {
  const { selectedProperty } = useAuth();
  const [unidades, setUnidades] = useState<UnidadeResponse[] | null>(null);
  const [unidadeId, setUnidadeId] = useState<string | null>(null);
  const [pessoas, setPessoas] = useState<MoradorResponse[] | null>(null);
  const [erro, setErro] = useState(false);

  const carregar = useCallback(async () => {
    if (!selectedProperty) return;
    setErro(false);
    try {
      const lista = await servicoPessoas.listarUnidades(selectedProperty.id);
      setUnidades(lista);
      const atual = lista.find((u) => u.id === unidadeId) ?? lista[0] ?? null;
      setUnidadeId(atual?.id ?? null);
      setPessoas(atual ? await servicoPessoas.listarPessoas(atual.id) : []);
    } catch {
      setErro(true);
    }
  }, [selectedProperty, unidadeId]);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar]),
  );

  // Troca de propriedade: nunca mostra pessoas da casa anterior.
  useEffect(() => {
    setUnidades(null);
    setPessoas(null);
    setUnidadeId(null);
  }, [selectedProperty?.id]);

  const garantirUnidade = useCallback(async (): Promise<string> => {
    if (unidadeId) return unidadeId;
    const padrao = unidadePadrao(selectedProperty!.tipo);
    const criada = await servicoPessoas.criarUnidade(selectedProperty!.id, padrao.tipo, padrao.identificacao);
    setUnidades([criada]);
    setUnidadeId(criada.id);
    return criada.id;
  }, [unidadeId, selectedProperty]);

  return {
    unidades,
    unidadeId,
    selecionarUnidade: (id: string) => {
      setUnidadeId(id);
      setPessoas(null);
    },
    pessoas,
    setPessoas,
    carregando: pessoas === null && !erro,
    erro,
    recarregar: carregar,
    garantirUnidade,
  };
}
