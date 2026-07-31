import { httpClient } from '../../../services/httpClient';
import type { DashboardAlocacao, ProvisionarRequest, TrocarRequest, Vinculo, VinculosPaginados } from '../types';

const BASE_URL = '/api/painel/provisionamentos';

/** Item mínimo para o seletor de equipamento do wizard — não é o módulo Equipamentos, ver nota abaixo. */
export interface EquipamentoParaAlocacao {
  id: string;
  nome: string;
  numeroSerie?: string | null;
  propriedadeNome?: string | null;
}

interface EquipamentosPaginadosBruto {
  itens: EquipamentoParaAlocacao[];
  totalItens: number;
}

/** Sprint 22B (ADR 0031) — única camada que fala HTTP neste módulo. */
export const provisionamentosAdaptador = {
  listarAtivos: (pagina: number, tamanhoPagina: number) =>
    httpClient.get<VinculosPaginados>(BASE_URL, { params: { pagina, tamanhoPagina } }).then((r) => r.data),

  obterDashboard: () => httpClient.get<DashboardAlocacao>(`${BASE_URL}/dashboard`).then((r) => r.data),

  listarHistorico: (equipamentoId: string) =>
    httpClient.get<Vinculo[]>(`${BASE_URL}/equipamentos/${equipamentoId}/historico`).then((r) => r.data),

  provisionar: (request: ProvisionarRequest) => httpClient.post<Vinculo>(BASE_URL, request).then((r) => r.data),

  trocar: (request: TrocarRequest) => httpClient.post<Vinculo>(`${BASE_URL}/trocar`, request).then((r) => r.data),

  desvincular: (equipamentoId: string) => httpClient.delete(`${BASE_URL}/equipamentos/${equipamentoId}`),

  /**
   * Sprint 22B (ADR 0031) — o wizard de ativação precisa de uma lista de equipamentos para
   * escolher qual alocar. Em vez de importar o módulo Equipamentos (violaria a Regra de Ouro de
   * isolamento entre módulos), este adaptador chama o mesmo endpoint (`api/painel/equipamentos`)
   * diretamente, só com os campos que o seletor precisa — duplicação deliberada e pequena, mesmo
   * espírito da duplicação já documentada no backend (`ResolverOuCriarModeloAsync`, ADR 0031).
   */
  listarEquipamentosParaAlocacao: () =>
    httpClient
      .get<EquipamentosPaginadosBruto>('/api/painel/equipamentos', { params: { pagina: 1, tamanhoPagina: 100 } })
      .then((r) => r.data.itens),
};
