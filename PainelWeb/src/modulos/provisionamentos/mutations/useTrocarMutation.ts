import { useMutation, useQueryClient } from '@tanstack/react-query';
import { provisionamentosAdaptador } from '../adaptadores/provisionamentosAdaptador';
import { provisionamentosKeys } from '../queryKeys';

export function useTrocarMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: provisionamentosAdaptador.trocar,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: provisionamentosKeys.all });
    },
  });
}
