import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { FabricanteEquipamento } from '../../../compartilhado/tipos/equipamento';
import { modelosAdaptador } from '../adaptadores/modelosAdaptador';
import type { CapacidadeEquipamento } from '../types';

const raiz = ['modelos-equipamento'] as const;

export function useModelosQuery(fabricante?: FabricanteEquipamento) {
  return useQuery({ queryKey: [...raiz, fabricante ?? 'todos'], queryFn: () => modelosAdaptador.listar(fabricante) });
}

export function useCriarModeloMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ fabricante, nome }: { fabricante: FabricanteEquipamento; nome: string }) => modelosAdaptador.criar(fabricante, nome),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: raiz }),
  });
}

export function useDefinirCapacidadesMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, capacidades }: { id: string; capacidades: CapacidadeEquipamento[] }) => modelosAdaptador.definirCapacidades(id, capacidades),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: raiz }),
  });
}
