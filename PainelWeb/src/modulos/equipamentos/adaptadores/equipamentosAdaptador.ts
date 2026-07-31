import { httpClient } from '../../../services/httpClient';
import type {
  AtualizarEquipamentoRequest,
  CriarEquipamentoRequest,
  EquipamentoAdmin,
  EquipamentosAdminPaginados,
  EstadoOperacionalEquipamento,
  FiltroEquipamentos,
} from '../types';

const BASE_URL = '/api/painel/equipamentos';

/** Sprint 22B (ADR 0031) — única camada que fala HTTP neste módulo; queries/mutations nunca chamam `httpClient` direto. */
export const equipamentosAdaptador = {
  listar: (pagina: number, tamanhoPagina: number, filtro: FiltroEquipamentos) =>
    httpClient
      .get<EquipamentosAdminPaginados>(BASE_URL, {
        params: {
          pagina,
          tamanhoPagina,
          busca: filtro.busca || undefined,
          fabricante: filtro.fabricante || undefined,
          estadoOperacional: filtro.estadoOperacional || undefined,
        },
      })
      .then((r) => r.data),

  obterPorId: (id: string) => httpClient.get<EquipamentoAdmin>(`${BASE_URL}/${id}`).then((r) => r.data),

  criar: (request: CriarEquipamentoRequest) =>
    httpClient.post<EquipamentoAdmin>(BASE_URL, request).then((r) => r.data),

  atualizar: (id: string, request: AtualizarEquipamentoRequest) =>
    httpClient.put<EquipamentoAdmin>(`${BASE_URL}/${id}`, request).then((r) => r.data),

  atualizarEstadoOperacional: (id: string, estadoOperacional: EstadoOperacionalEquipamento) =>
    httpClient
      .patch<EquipamentoAdmin>(`${BASE_URL}/${id}/estado-operacional`, { estadoOperacional })
      .then((r) => r.data),

  excluir: (id: string) => httpClient.delete(`${BASE_URL}/${id}`),
};
