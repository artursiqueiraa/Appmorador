import { httpClient } from '../../../services/httpClient';
import type { MembroEquipe, NovoMembroEquipe } from '../types';

const BASE_URL = '/api/usuarios-internos';

/** Akteck Seg (ADR 0035) — endpoints já existentes desde a Sprint 21 (ADR 0021), exclusivos de Master. */
export const equipeAdaptador = {
  listar: () => httpClient.get<MembroEquipe[]>(BASE_URL).then((r) => r.data),
  criar: (dados: NovoMembroEquipe) => httpClient.post<MembroEquipe>(BASE_URL, dados).then((r) => r.data),
  desativar: (id: string) => httpClient.post(`${BASE_URL}/${id}/desativar`).then(() => undefined),
};
