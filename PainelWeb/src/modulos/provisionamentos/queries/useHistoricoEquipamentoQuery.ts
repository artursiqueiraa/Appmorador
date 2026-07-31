import { useQuery } from '@tanstack/react-query';
import { provisionamentosAdaptador } from '../adaptadores/provisionamentosAdaptador';
import { provisionamentosKeys } from '../queryKeys';

export function useHistoricoEquipamentoQuery(equipamentoId: string | null) {
  return useQuery({
    queryKey: provisionamentosKeys.historico(equipamentoId ?? ''),
    queryFn: () => provisionamentosAdaptador.listarHistorico(equipamentoId!),
    enabled: Boolean(equipamentoId),
  });
}
