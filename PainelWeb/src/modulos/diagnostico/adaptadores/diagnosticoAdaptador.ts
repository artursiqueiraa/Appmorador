import { httpClient } from '../../../services/httpClient';
import type { DiagnosticoEquipamentosPaginados } from '../types';

const BASE_URL = '/api/diagnostico';

/** Sprint 22B (ADR 0031) — estritamente leitura, nunca chama nenhum endpoint de escrita. */
export const diagnosticoAdaptador = {
  obterStatusEquipamentos: (pagina: number, tamanhoPagina: number) =>
    httpClient
      .get<DiagnosticoEquipamentosPaginados>(`${BASE_URL}/equipamentos/status`, { params: { pagina, tamanhoPagina } })
      .then((r) => r.data),
};
