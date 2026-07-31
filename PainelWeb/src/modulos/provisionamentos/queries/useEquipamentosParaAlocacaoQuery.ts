import { useQuery } from '@tanstack/react-query';
import { provisionamentosAdaptador } from '../adaptadores/provisionamentosAdaptador';
import { provisionamentosKeys } from '../queryKeys';

export function useEquipamentosParaAlocacaoQuery(habilitado: boolean) {
  return useQuery({
    queryKey: [...provisionamentosKeys.all, 'equipamentos-para-alocacao'],
    queryFn: provisionamentosAdaptador.listarEquipamentosParaAlocacao,
    enabled: habilitado,
  });
}
