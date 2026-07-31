import { useQuery } from '@tanstack/react-query';
import { provisionamentosAdaptador } from '../adaptadores/provisionamentosAdaptador';
import { provisionamentosKeys } from '../queryKeys';

export function useProvisionamentosAtivosQuery(pagina: number, tamanhoPagina: number) {
  return useQuery({
    queryKey: provisionamentosKeys.ativos(pagina, tamanhoPagina),
    queryFn: () => provisionamentosAdaptador.listarAtivos(pagina, tamanhoPagina),
  });
}
