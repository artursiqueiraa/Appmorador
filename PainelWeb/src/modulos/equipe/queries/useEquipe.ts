import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { equipeAdaptador } from '../adaptadores/equipeAdaptador';
import type { NovoMembroEquipe } from '../types';

const chave = ['equipe-interna'] as const;

export function useEquipeQuery() {
  return useQuery({ queryKey: chave, queryFn: equipeAdaptador.listar });
}

export function useCriarMembroMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (dados: NovoMembroEquipe) => equipeAdaptador.criar(dados),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: chave }),
  });
}

export function useDesativarMembroMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => equipeAdaptador.desativar(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: chave }),
  });
}
