import { httpClient } from '../../../services/httpClient';
import type { CriarNovaInstalacaoRequest, EquipamentoLivre, NovaInstalacaoResponse } from '../types';

interface EquipamentoBruto extends EquipamentoLivre {
  excluido: boolean;
}

/**
 * Akteck Seg (ADR 0035). "Equipamento livre" = cadastrado, não excluído e sem vínculo ativo.
 * Cruza os dois endpoints do Painel aqui mesmo em vez de importar outro módulo (isolamento
 * entre módulos, ADR 0031). Amostra de 100 itens, o máximo que o backend pagina.
 */
export const novaInstalacaoAdaptador = {
  criar: (request: CriarNovaInstalacaoRequest) =>
    httpClient.post<NovaInstalacaoResponse>('/api/painel/novas-instalacoes', request).then((r) => r.data),

  listarEquipamentosLivres: async (): Promise<EquipamentoLivre[]> => {
    const [equipamentos, vinculos] = await Promise.all([
      httpClient
        .get<{ itens: EquipamentoBruto[] }>('/api/painel/equipamentos', { params: { pagina: 1, tamanhoPagina: 100 } })
        .then((r) => r.data.itens),
      httpClient
        .get<{ itens: { equipamentoId: string }[] }>('/api/painel/provisionamentos', { params: { pagina: 1, tamanhoPagina: 100 } })
        .then((r) => r.data.itens),
    ]);
    const vinculados = new Set(vinculos.map((v) => v.equipamentoId));
    return equipamentos
      .filter((e) => !e.excluido && !vinculados.has(e.id))
      .map(({ id, nome, fabricante, numeroSerie }) => ({ id, nome, fabricante, numeroSerie }));
  },
};
