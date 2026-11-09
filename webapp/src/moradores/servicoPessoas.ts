import { api } from '../api/client';
import { anexarFoto } from './arquivoFoto';
import type {
  CredencialResponse,
  MoradorResponse,
  SalvarMoradorRequest,
  TipoUnidade,
  TipoVeiculo,
  UnidadeResponse,
  VeiculoResponse,
} from '../api/types';

/**
 * Akteck Seg (ADR 0036) — única porta de entrada HTTP da aba Moradores. As telas nunca
 * chamam `api` direto, então trocar um endpoint muda um lugar só.
 */
export const servicoPessoas = {
  listarUnidades: (propriedadeId: string) => api.get<UnidadeResponse[]>(`/api/properties/${propriedadeId}/unidades`),

  criarUnidade: (propriedadeId: string, tipo: TipoUnidade, identificacao: string) =>
    api.post<UnidadeResponse>(`/api/properties/${propriedadeId}/unidades`, { tipo, identificacao }),

  listarPessoas: (unidadeId: string) => api.get<MoradorResponse[]>(`/api/unidades/${unidadeId}/moradores`),

  criarPessoa: (unidadeId: string, dados: SalvarMoradorRequest) =>
    api.post<MoradorResponse>(`/api/unidades/${unidadeId}/moradores`, dados),

  atualizarPessoa: (moradorId: string, dados: SalvarMoradorRequest) =>
    api.put<MoradorResponse>(`/api/moradores/${moradorId}`, { status: 'Ativo', ...dados }),

  excluirPessoa: (moradorId: string) => api.delete(`/api/moradores/${moradorId}`),

  listarVeiculos: (moradorId: string) => api.get<VeiculoResponse[]>(`/api/moradores/${moradorId}/veiculos`),

  criarVeiculo: (moradorId: string, dados: { placa: string; tipo: TipoVeiculo; marca?: string; modelo?: string; cor?: string }) =>
    api.post<VeiculoResponse>(`/api/moradores/${moradorId}/veiculos`, dados),

  excluirVeiculo: (veiculoId: string) => api.delete(`/api/veiculos/${veiculoId}`),

  /**
   * Envia a foto de rosto e garante uma credencial Facial ativa para a pessoa. A foto é a
   * fonte do cadastro facial; a credencial é o que o controle de acesso consulta.
   * `uri` é o que o seletor de imagem devolve (data/blob URI no navegador, `file://` no celular).
   */
  async cadastrarRosto(moradorId: string, uri: string): Promise<void> {
    const formulario = new FormData();
    await anexarFoto(formulario, 'foto', uri);
    await api.enviarArquivo(`/api/moradores/${moradorId}/foto`, formulario);

    const credenciais = await api.get<CredencialResponse[]>(`/api/moradores/${moradorId}/credenciais`);
    if (!credenciais.some((c) => c.tipo === 'Facial' && c.status === 'Ativa')) {
      await api.post<CredencialResponse>(`/api/moradores/${moradorId}/credenciais`, { tipo: 'Facial' });
    }
  },

  removerRosto: (moradorId: string) => api.delete(`/api/moradores/${moradorId}/foto`),
};
