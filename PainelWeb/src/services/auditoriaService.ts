import { httpClient } from './httpClient';
import type { AuditoriaMasterItem } from '../types/api';

export const auditoriaService = {
  listarRecentes: (quantidade = 20) =>
    httpClient.get<AuditoriaMasterItem[]>('/api/auditoria', { params: { quantidade } }).then((r) => r.data),

  listarPorUsuario: (usuarioId: string, inicio?: string, fim?: string) =>
    httpClient
      .get<AuditoriaMasterItem[]>(`/api/auditoria/usuarios/${usuarioId}`, { params: { inicio, fim } })
      .then((r) => r.data),

  listarPorPropriedade: (propriedadeId: string, inicio?: string, fim?: string) =>
    httpClient
      .get<AuditoriaMasterItem[]>(`/api/auditoria/propriedades/${propriedadeId}`, { params: { inicio, fim } })
      .then((r) => r.data),
};
