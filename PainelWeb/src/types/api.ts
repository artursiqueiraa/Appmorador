/** Sprint 22A — tipos espelhando 1:1 os DTOs reais do backend (ver docs/painel/mapeamento-api.md). */

export type RoleSistema = 'Master' | 'Tecnico' | 'Suporte';
export type PerfilPropriedade = 'Administrador' | 'Morador';

export interface EntrarRequest {
  email: string;
  senha: string;
}

export interface EntrarResponse {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  usuarioId: string;
  nome: string;
  email: string;
}

export interface ImpersonarRequest {
  propriedadeId: string;
}

export interface ImpersonarResponse {
  accessToken: string;
  expiresInSeconds: number;
  propriedadeId: string;
  propriedadeNome: string;
  clienteNome: string;
}

export interface ProprietarioResponse {
  id: string;
  nome: string;
  email: string;
  ativo: boolean;
  createdAtUtc: string;
  quantidadePropriedades: number;
}

export interface ProprietariosPaginadosResponse {
  itens: ProprietarioResponse[];
  paginaAtual: number;
  totalPaginas: number;
  totalItens: number;
}

export interface NovosClientesPorMesItem {
  mes: string;
  quantidade: number;
}

export interface DashboardOperacionalResponse {
  totalClientes: number;
  totalPropriedades: number;
  totalEquipamentos: number;
  totalEquipamentosOffline: number;
  novosClientesPorMes: NovosClientesPorMesItem[];
  propriedadesPorTipo: Record<string, number>;
  equipamentosPorStatus: Record<string, number>;
}

export type TipoAcaoAuditoria =
  | 'Login'
  | 'Logout'
  | 'ImpersonationInicio'
  | 'ImpersonationFim'
  | 'Criar'
  | 'Editar'
  | 'Excluir'
  | 'Visualizar'
  | 'FalhaAutorizacao';

/** Espelha a entidade `AuditoriaMaster` — o Controller devolve ela diretamente, sem DTO próprio. */
export interface AuditoriaMasterItem {
  id: string;
  usuarioId: string;
  usuarioNome: string;
  acao: TipoAcaoAuditoria;
  entidade?: string | null;
  entidadeId?: string | null;
  detalhes?: string | null;
  ipAddress?: string | null;
  dataHoraUtc: string;
}

export interface PropriedadeResponse {
  id: string;
  nome: string;
  tipo: string;
  endereco?: string | null;
  perfil: PerfilPropriedade;
  permissoes: string[];
  features: string[];
}

export interface UsuarioInternoResponse {
  id: string;
  nome: string;
  email: string;
  roleGlobal: RoleSistema;
  ativo: boolean;
  createdAtUtc: string;
}

export interface ProvisionamentoResponse {
  id: string;
  propriedadeId: string;
  nome: string;
  template: 'Residencia' | 'Loja' | 'Escritorio';
  status: 'Rascunho' | 'Ativo' | 'Arquivado';
  createdAtUtc: string;
  atualizadoEmUtc?: string | null;
}

export interface ApiErrorBody {
  error?: string;
}
