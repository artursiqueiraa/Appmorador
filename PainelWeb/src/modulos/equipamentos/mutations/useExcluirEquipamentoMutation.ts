import { useMutation, useQueryClient } from '@tanstack/react-query';
import { equipamentosAdaptador } from '../adaptadores/equipamentosAdaptador';
import { equipamentosKeys } from '../queryKeys';

export function useExcluirEquipamentoMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => equipamentosAdaptador.excluir(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: equipamentosKeys.all });
    },
  });
}
