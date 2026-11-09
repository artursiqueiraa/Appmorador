import { httpClient } from '../../../services/httpClient';
import type { RecursoPropriedade } from '../types';

/** Akteck Seg (ADR 0035) — `api/properties/{id}/features` (Sprint 21, ADR 0026), exclusivo de Master/Técnico. */
export const recursosAdaptador = {
  listarAtivos: (propriedadeId: string) =>
    httpClient.get<RecursoPropriedade[]>(`/api/properties/${propriedadeId}/features`).then((r) => r.data),
  definir: (propriedadeId: string, recurso: RecursoPropriedade, ativo: boolean) =>
    httpClient.put<RecursoPropriedade[]>(`/api/properties/${propriedadeId}/features/${recurso}`, { ativo }).then((r) => r.data),
};
