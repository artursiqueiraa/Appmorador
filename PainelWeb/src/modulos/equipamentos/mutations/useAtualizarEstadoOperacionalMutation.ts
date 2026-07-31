import { useMutation, useQueryClient } from '@tanstack/react-query';
import { equipamentosAdaptador } from '../adaptadores/equipamentosAdaptador';
import { equipamentosKeys } from '../queryKeys';
import type { EstadoOperacionalEquipamento } from '../types';

export function useAtualizarEstadoOperacionalMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, estadoOperacional }: { id: string; estadoOperacional: EstadoOperacionalEquipamento }) =>
      equipamentosAdaptador.atualizarEstadoOperacional(id, estadoOperacional),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: equipamentosKeys.all });
    },
  });
}
