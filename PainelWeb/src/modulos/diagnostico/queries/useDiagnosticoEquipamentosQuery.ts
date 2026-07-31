import { useQuery } from '@tanstack/react-query';
import { diagnosticoAdaptador } from '../adaptadores/diagnosticoAdaptador';
import { diagnosticoKeys } from '../queryKeys';

/** `intervaloPollingMs` nulo desliga o polling automático (atualização só manual/ao trocar de página). */
export function useDiagnosticoEquipamentosQuery(pagina: number, tamanhoPagina: number, intervaloPollingMs: number | null) {
  return useQuery({
    queryKey: diagnosticoKeys.status(pagina, tamanhoPagina),
    queryFn: () => diagnosticoAdaptador.obterStatusEquipamentos(pagina, tamanhoPagina),
    refetchInterval: intervaloPollingMs ?? false,
  });
}
