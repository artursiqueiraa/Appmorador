import { useMutation, useQueryClient } from '@tanstack/react-query';
import { provisionamentosAdaptador } from '../adaptadores/provisionamentosAdaptador';
import { provisionamentosKeys } from '../queryKeys';

export function useDesvincularMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (equipamentoId: string) => provisionamentosAdaptador.desvincular(equipamentoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: provisionamentosKeys.all });
    },
  });
}
