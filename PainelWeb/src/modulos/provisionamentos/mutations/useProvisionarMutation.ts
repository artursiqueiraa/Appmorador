import { useMutation, useQueryClient } from '@tanstack/react-query';
import { provisionamentosAdaptador } from '../adaptadores/provisionamentosAdaptador';
import { provisionamentosKeys } from '../queryKeys';

export function useProvisionarMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: provisionamentosAdaptador.provisionar,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: provisionamentosKeys.all });
    },
  });
}
