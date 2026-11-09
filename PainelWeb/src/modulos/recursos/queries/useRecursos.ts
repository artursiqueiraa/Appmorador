import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { recursosAdaptador } from '../adaptadores/recursosAdaptador';
import type { RecursoPropriedade } from '../types';

const chave = (propriedadeId: string) => ['recursos-propriedade', propriedadeId] as const;

export function useRecursosQuery(propriedadeId: string) {
  return useQuery({ queryKey: chave(propriedadeId), queryFn: () => recursosAdaptador.listarAtivos(propriedadeId) });
}

export function useDefinirRecursoMutation(propriedadeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ recurso, ativo }: { recurso: RecursoPropriedade; ativo: boolean }) => recursosAdaptador.definir(propriedadeId, recurso, ativo),
    onSuccess: (ativos) => queryClient.setQueryData(chave(propriedadeId), ativos),
  });
}
