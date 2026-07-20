import type { TipoPropriedade } from '../components/TipoPropriedadeSelector';

export interface EntrarResponse {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  usuarioId: string;
  nome: string;
  email: string;
}

export interface PropriedadeResponse {
  id: string;
  nome: string;
  tipo: TipoPropriedade;
  endereco?: string | null;
}

export interface DashboardResponse {
  nome: string;
  tipo: TipoPropriedade;
  statusSeguranca: string;
  pontuacaoSaude: number;
  ultimoEvento?: string | null;
  ultimoEventoEmUtc?: string | null;
  quantidadeCentrais: number;
  quantidadeGravadores: number;
  quantidadeCameras: number;
  quantidadeSensores: number;
  quantidadePessoas: number;
}

export interface EventoResponse {
  id: string;
  titulo: string;
  descricao?: string | null;
  ocorridoEmUtc: string;
  destaque: boolean;
}

export interface EventosPaginadosResponse {
  itens: EventoResponse[];
  paginaAtual: number;
  totalPaginas: number;
  totalItens: number;
}
