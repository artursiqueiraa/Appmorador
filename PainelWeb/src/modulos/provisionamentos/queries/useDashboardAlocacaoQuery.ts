import { useQuery } from '@tanstack/react-query';
import { provisionamentosAdaptador } from '../adaptadores/provisionamentosAdaptador';
import { provisionamentosKeys } from '../queryKeys';

export function useDashboardAlocacaoQuery() {
  return useQuery({
    queryKey: provisionamentosKeys.dashboard(),
    queryFn: provisionamentosAdaptador.obterDashboard,
  });
}
