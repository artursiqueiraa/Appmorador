import { httpClient } from '../../../services/httpClient';
import type { FabricanteEquipamento } from '../../../compartilhado/tipos/equipamento';
import type { CapacidadeEquipamento, ModeloEquipamento } from '../types';

const BASE_URL = '/api/modelos-equipamento';

/** Akteck Seg (ADR 0035) — catálogo existente desde a Sprint 21 (ADR 0027), exclusivo de Master/Técnico. */
export const modelosAdaptador = {
  listar: (fabricante?: FabricanteEquipamento) =>
    httpClient.get<ModeloEquipamento[]>(BASE_URL, { params: { fabricante } }).then((r) => r.data),
  criar: (fabricante: FabricanteEquipamento, nome: string) =>
    httpClient.post<ModeloEquipamento>(BASE_URL, { fabricante, nome }).then((r) => r.data),
  definirCapacidades: (id: string, capacidades: CapacidadeEquipamento[]) =>
    httpClient.put<ModeloEquipamento>(`${BASE_URL}/${id}/capacidades`, { capacidades }).then((r) => r.data),
};
