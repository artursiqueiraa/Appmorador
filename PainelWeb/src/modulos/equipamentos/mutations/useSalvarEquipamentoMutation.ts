import { useMutation, useQueryClient } from '@tanstack/react-query';
import { equipamentosAdaptador } from '../adaptadores/equipamentosAdaptador';
import { equipamentosKeys } from '../queryKeys';
import type { AtualizarEquipamentoRequest, CriarEquipamentoRequest } from '../types';

export type SalvarEquipamentoInput =
  | { modo: 'criar'; request: CriarEquipamentoRequest }
  | { modo: 'editar'; id: string; request: AtualizarEquipamentoRequest };

/** Sprint 22B (ADR 0031) — criar/editar unificados numa só mutation (mesmo formulário atende os dois modos). */
export function useSalvarEquipamentoMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: SalvarEquipamentoInput) =>
      input.modo === 'criar'
        ? equipamentosAdaptador.criar(input.request)
        : equipamentosAdaptador.atualizar(input.id, input.request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: equipamentosKeys.all });
    },
  });
}
