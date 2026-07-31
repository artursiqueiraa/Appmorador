import { useQuery } from '@tanstack/react-query';
import { equipamentosAdaptador } from '../adaptadores/equipamentosAdaptador';
import { equipamentosKeys } from '../queryKeys';
import type { FiltroEquipamentos } from '../types';

export function useEquipamentosQuery(pagina: number, tamanhoPagina: number, filtro: FiltroEquipamentos) {
  return useQuery({
    queryKey: equipamentosKeys.lista(pagina, tamanhoPagina, filtro),
    queryFn: () => equipamentosAdaptador.listar(pagina, tamanhoPagina, filtro),
  });
}
