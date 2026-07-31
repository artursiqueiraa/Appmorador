import { httpClient } from './httpClient';
import type { ProprietariosPaginadosResponse } from '../types/api';

export interface PropriedadeResumo {
  id: string;
  nome: string;
  tipo: string;
}

export interface ProprietarioDetalhe {
  id: string;
  nome: string;
  email: string;
  ativo: boolean;
  createdAtUtc: string;
  propriedades: PropriedadeResumo[];
}

export const proprietariosService = {
  listar: (pagina: number, tamanhoPagina: number, busca?: string) =>
    httpClient
      .get<ProprietariosPaginadosResponse>('/api/proprietarios', { params: { pagina, tamanhoPagina, busca } })
      .then((r) => r.data),

  obterDetalhe: (id: string) => httpClient.get<ProprietarioDetalhe>(`/api/proprietarios/${id}`).then((r) => r.data),
};
