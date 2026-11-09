import { api } from '../api/client';
import type { AlertaCriticoResponse, ConfiguracaoAlertasResponse, ContatoEmergenciaResponse } from '../api/types';

/** Sprint 24 (ADR 0040) — chamadas da API de alertas críticos e contatos de emergência. */
export const servicoAlertas = {
  listarAbertos: (propriedadeId: string) =>
    api.get<AlertaCriticoResponse[]>(`/api/properties/${propriedadeId}/alertas?apenasAbertos=true`),

  obter: (alertaId: string) => api.get<AlertaCriticoResponse>(`/api/alertas/${alertaId}`),

  confirmar: (alertaId: string) => api.post<AlertaCriticoResponse>(`/api/alertas/${alertaId}/confirmar`),

  acionarPanico: (propriedadeId: string) => api.post<AlertaCriticoResponse>(`/api/properties/${propriedadeId}/panico`),

  obterConfiguracao: (propriedadeId: string) =>
    api.get<ConfiguracaoAlertasResponse>(`/api/properties/${propriedadeId}/alertas/configuracao`),

  salvarConfiguracao: (propriedadeId: string, minutosEscalonamento: number) =>
    api.put<ConfiguracaoAlertasResponse>(`/api/properties/${propriedadeId}/alertas/configuracao`, { minutosEscalonamento }),

  listarContatos: (propriedadeId: string) =>
    api.get<ContatoEmergenciaResponse[]>(`/api/properties/${propriedadeId}/contatos-emergencia`),

  criarContato: (propriedadeId: string, nome: string, telefone: string) =>
    api.post<ContatoEmergenciaResponse>(`/api/properties/${propriedadeId}/contatos-emergencia`, { nome, telefone }),

  reordenarContatos: (propriedadeId: string, ids: string[]) =>
    api.put<ContatoEmergenciaResponse[]>(`/api/properties/${propriedadeId}/contatos-emergencia/ordem`, { ids }),

  removerContato: (contatoId: string) => api.delete<void>(`/api/contatos-emergencia/${contatoId}`),
};

/** "11987654321" → "(11) 98765-4321"; "1134567890" → "(11) 3456-7890". Outros formatos ficam como vieram. */
export function formatarTelefone(digitos: string): string {
  const d = digitos.startsWith('55') && digitos.length >= 12 ? digitos.slice(2) : digitos;
  if (d.length === 11) {
    return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  }
  if (d.length === 10) {
    return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  }
  return digitos;
}
