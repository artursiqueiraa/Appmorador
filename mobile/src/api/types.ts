import type { TipoPropriedade } from '../components/TipoPropriedadeSelector';

export interface EntrarResponse {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  usuarioId: string;
  nome: string;
  email: string;
}

/** Sprint 21 (ADR 0021) — mesmos nomes de `PerfilPropriedade` do backend (serializado como string). Só 'Administrador' é alcançável hoje (Morador ainda não tem login próprio). */
export type PerfilPropriedade = 'Administrador' | 'Morador';

/** Sprint 21 (ADR 0025) — mesmos nomes de `PermissaoFuncionalidade` do backend. */
export type PermissaoFuncionalidade =
  | 'CadastrarMorador'
  | 'CadastrarFacial'
  | 'CadastrarTag'
  | 'CadastrarCamera'
  | 'AlterarLeitor'
  | 'AlterarGravador'
  | 'ConfigurarPgm'
  | 'ConfigurarIntegracoes'
  | 'VerLogs'
  | 'AbrirPortao'
  | 'VerCameras'
  | 'CriarVisitante';

/** Sprint 21 (ADR 0026) — mesmos nomes de `FeatureFlag` do backend. */
export type FeatureFlag = 'Facial' | 'Cameras' | 'Pgm' | 'Push' | 'Snapshot' | 'InterfoneSip' | 'StreamingAoVivo' | 'Ia';

export interface PropriedadeResponse {
  id: string;
  nome: string;
  tipo: TipoPropriedade;
  endereco?: string | null;
  /** Sprint 21 (ADR 0021) — perfil do usuário logado NESTA propriedade (nunca um papel global). */
  perfil: PerfilPropriedade;
  /** Sprint 21 (ADR 0025) — o que este usuário pode fazer nesta propriedade (varia por plano/concessão, não só pelo perfil). */
  permissoes: PermissaoFuncionalidade[];
  /** Sprint 21 (ADR 0026) — o que esta propriedade contratou (ortogonal a permissões — "pode fazer" vs. "existe pra fazer"). */
  features: FeatureFlag[];
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
  quantidadeUnidades: number;
  quantidadePessoas: number;
  quantidadeCredenciais: number;
  quantidadeCredenciaisAtivas: number;
  quantidadeCredenciaisSuspensas: number;
  quantidadePontosAcesso: number;
  quantidadeVisitantesAtivos: number;
  quantidadeAutorizacoesPendentes: number;
  quantidadeAutorizacoesExpiradas: number;
  quantidadeVeiculos: number;
  quantidadeVeiculosAtivos: number;
  quantidadeVagas: number;
  quantidadeVagasLivres: number;
  quantidadeVagasOcupadas: number;
  quantidadeEntregasPendentes: number;
  quantidadeEntregasDisponiveis: number;
  quantidadeEntregasRetiradas: number;
  quantidadeCorrespondenciasCadastradas: number;
  quantidadeEquipamentosOnline: number;
  quantidadeEquipamentosOffline: number;
  ultimaSincronizacaoUtc?: string | null;
  ultimoEventoEquipamentoRecebidoUtc?: string | null;
  quantidadeCentraisJflOnline: number;
  quantidadeCentraisJflOffline: number;
  quantidadeParticoesArmadas: number;
  quantidadeParticoesDesarmadas: number;
  quantidadeProblemasAtivosJfl: number;
  saude: EstadoOperacional;
  quantidadeEventosHoje: number;
  quantidadeAlarmesAtivos: number;
  ultimaAtualizacaoOperacionalUtc?: string | null;
}

export type TipoUnidade = 'Casa' | 'Apartamento' | 'Loja' | 'SalaComercial' | 'Galpao' | 'Quiosque' | 'Escritorio' | 'Outro';

export interface UnidadeResponse {
  id: string;
  propriedadeId: string;
  tipo: TipoUnidade;
  identificacao: string;
}

export type StatusMorador = 'Ativo' | 'Inativo';

export interface MoradorResponse {
  id: string;
  unidadeId: string;
  nome: string;
  fotoPath?: string | null;
  telefone?: string | null;
  email?: string | null;
  documento?: string | null;
  status: StatusMorador;
  observacoes?: string | null;
}

export type TipoCredencial = 'Facial' | 'TagRfid' | 'QrCode' | 'Pin' | 'Biometria' | 'ChaveVirtual';

export type StatusCredencial = 'Ativa' | 'Suspensa' | 'Expirada' | 'Revogada';

export interface CredencialResponse {
  id: string;
  moradorId: string;
  tipo: TipoCredencial;
  status: StatusCredencial;
}

export type TipoPontoAcesso = 'Geral' | 'Veicular';

export interface PontoAcessoResponse {
  id: string;
  propriedadeId: string;
  nome: string;
  tipo: TipoPontoAcesso;
}

/** Dia individual — o backend serializa DiaSemana (enum [Flags]) como nomes separados por vírgula (ex.: "Segunda, Terca") ou "Todos". */
export type DiaSemanaToken = 'Segunda' | 'Terca' | 'Quarta' | 'Quinta' | 'Sexta' | 'Sabado' | 'Domingo';

export interface PermissaoAcessoResponse {
  id: string;
  credencialId: string;
  pontoAcessoId: string;
  pontoAcessoNome: string;
  diasPermitidos: string;
  horarioInicial?: string | null;
  horarioFinal?: string | null;
  dataInicial?: string | null;
  dataFinal?: string | null;
}

export interface VisitanteResponse {
  id: string;
  propriedadeId: string;
  nome: string;
  documento?: string | null;
  telefone?: string | null;
  fotoPath?: string | null;
  observacoes?: string | null;
}

export type TipoVisita = 'Visitante' | 'PrestadorServico' | 'Entregador' | 'Evento' | 'Temporario';

export type StatusAutorizacao = 'Pendente' | 'Ativa' | 'Expirada' | 'Cancelada' | 'Utilizada';

export interface AutorizacaoResponse {
  id: string;
  moradorResponsavelId: string;
  moradorResponsavelNome: string;
  unidadeId: string;
  unidadeIdentificacao: string;
  visitanteId: string;
  visitanteNome: string;
  tipo: TipoVisita;
  dataInicial: string;
  dataFinal: string;
  horarioInicial?: string | null;
  horarioFinal?: string | null;
  status: StatusAutorizacao;
}

export type TipoVeiculo = 'Carro' | 'Moto' | 'Caminhonete' | 'Van' | 'Caminhao' | 'Bicicleta' | 'Outro';

export type StatusVeiculo = 'Ativo' | 'Suspenso' | 'Inativo';

export interface VeiculoResponse {
  id: string;
  moradorId: string;
  placa: string;
  marca?: string | null;
  modelo?: string | null;
  cor?: string | null;
  ano?: number | null;
  observacoes?: string | null;
  tipo: TipoVeiculo;
  status: StatusVeiculo;
}

export type TipoVaga = 'Morador' | 'Visitante' | 'Comercial' | 'Servico';

export type StatusVaga = 'Livre' | 'Ocupada' | 'Bloqueada' | 'Reservada';

export interface VagaResponse {
  id: string;
  propriedadeId: string;
  numero: string;
  bloco?: string | null;
  andar?: string | null;
  coberta: boolean;
  tipo: TipoVaga;
  observacoes?: string | null;
  status: StatusVaga;
}

export interface VinculoVeiculoVagaResponse {
  id: string;
  veiculoId: string;
  vagaId: string;
  vagaNumero: string;
  dataInicioUtc: string;
  dataFimUtc?: string | null;
}

export type TipoEntrega = 'Correspondencia' | 'Encomenda' | 'Delivery' | 'Documento' | 'Mercado' | 'Outro';

export type StatusEntrega = 'AguardandoRecebimento' | 'DisponivelParaRetirada' | 'Retirada' | 'Cancelada';

export interface EntregaResponse {
  id: string;
  moradorDestinatarioId: string;
  moradorDestinatarioNome: string;
  unidadeId: string;
  unidadeIdentificacao: string;
  tipo: TipoEntrega;
  descricao?: string | null;
  recebidoPor?: string | null;
  dataRecebimentoUtc?: string | null;
  dataRetiradaUtc?: string | null;
  observacoes?: string | null;
  status: StatusEntrega;
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

export type FabricanteEquipamento = 'ControlId' | 'Jfl' | 'Intelbras' | 'Hikvision' | 'Dahua' | 'Outro';

export type StatusEquipamento = 'Desconhecido' | 'Online' | 'Offline';

export interface EquipamentoResponse {
  id: string;
  propriedadeId: string;
  nome: string;
  modelo?: string | null;
  fabricante: FabricanteEquipamento;
  ip?: string | null;
  porta?: number | null;
  usuario?: string | null;
  identificador?: string | null;
  status: StatusEquipamento;
  ultimaSincronizacaoUtc?: string | null;
}

export interface TesteConexaoResponse {
  sucesso: boolean;
  mensagemErro?: string | null;
}

export interface InformacoesEquipamentoResponse {
  versao: string;
  nomeDispositivo?: string | null;
  numeroSerie?: string | null;
}

export interface SincronizacaoResponse {
  quantidadeProcessada: number;
}

export interface ImportacaoEventosResponse {
  quantidadeImportada: number;
}

export interface CentralJflResponse {
  equipamentoId: string;
  propriedadeId: string;
  nome: string;
  modelo?: string | null;
  numeroSerie: string;
  status: StatusEquipamento;
  ultimaSincronizacaoUtc?: string | null;
  centralVinculadaId?: string | null;
  centralVinculadaNome?: string | null;
  quantidadeParticoesArmadas?: number | null;
  quantidadeParticoesDesarmadas?: number | null;
  temProblemaAtivo?: boolean | null;
}

export interface ResultadoTesteConexaoJfl {
  sucesso: boolean;
  mensagemErro?: string | null;
}

export interface ParticaoStatusInfo {
  numero: number;
  desabilitada: boolean;
  armada: boolean;
  armadaStay: boolean;
  emDisparo: boolean;
}

export interface ZonaStatusInfo {
  numero: number;
  estado: string;
  permiteInibir: boolean;
}

export interface PgmStatusInfo {
  numero: number;
  acionada: boolean;
  permitida: boolean;
}

export interface StatusCentralJflInfo {
  dataHoraCentral?: string | null;
  bateriaTipo: string;
  bateriaPercentual?: number | null;
  bateriaTensaoAproximada?: number | null;
  eletrificadorArmado: boolean;
  particoes: ParticaoStatusInfo[];
  zonas: ZonaStatusInfo[];
  pgms: PgmStatusInfo[];
  problemasAtivos: string[];
}

export interface ResultadoComandoJfl {
  sucesso: boolean;
  mensagemErro?: string | null;
  statusResultante?: StatusCentralJflInfo | null;
}

export interface CentralIntelbrasResponse {
  equipamentoId: string;
  propriedadeId: string;
  nome: string;
  modelo?: string | null;
  status: StatusEquipamento;
  ultimaSincronizacaoUtc?: string | null;
  quantidadeParticoesArmadas?: number | null;
  quantidadeParticoesDesarmadas?: number | null;
  temProblemaAtivo?: boolean | null;
}

export interface ResultadoTesteConexaoIntelbras {
  sucesso: boolean;
  mensagemErro?: string | null;
}

export interface ParticaoIntelbrasStatusInfo {
  numero: number;
  armada: boolean;
}

export interface StatusCentralIntelbrasInfo {
  particoes: ParticaoIntelbrasStatusInfo[];
  temProblemaAtivo: boolean;
}

export interface ResultadoComandoIntelbras {
  sucesso: boolean;
  mensagemErro?: string | null;
  statusResultante?: StatusCentralIntelbrasInfo | null;
}

export interface ImportacaoEventosIntelbrasResponse {
  quantidadeImportada: number;
}

export type EstadoOperacional = 'Saudavel' | 'Atencao' | 'Critico' | 'Offline';

export interface EquipamentoSaudeResponse {
  equipamentoId: string;
  nome: string;
  fabricante: FabricanteEquipamento;
  estado: EstadoOperacional;
}

/** Sprint 20 (ADR 0024) — sem monitoramento contínuo: só muda no momento de uma tentativa de captura (alarme ou sob demanda). */
export type StatusCamera = 'Desconhecido' | 'Online' | 'Offline';

export interface CameraResponse {
  id: string;
  nome: string;
  status: StatusCamera;
  /** Caminho da Api (relativo) — o mobile monta a URL completa com o host + anexa o Bearer token (ver useAuthHeader). Null se nunca houve captura com sucesso. */
  ultimaImagemUrl?: string | null;
  ultimaVezVistaUtc?: string | null;
}

export interface CameraSnapshotResponse {
  sucesso: boolean;
  mensagemErro?: string | null;
  ultimaImagemUrl?: string | null;
  capturadaEmUtc?: string | null;
  status: StatusCamera;
}

export interface CameraStatusResponse {
  status: StatusCamera;
  ultimaTentativaCapturaUtc?: string | null;
  ultimoSucessoCapturaUtc?: string | null;
  motivoIndisponibilidade?: string | null;
}

export interface SnapshotOperacionalResponse {
  geradoEmUtc: string;
  saude: EstadoOperacional;
  quantidadeEquipamentosOnline: number;
  quantidadeEquipamentosOffline: number;
  ultimaComunicacaoUtc?: string | null;
  quantidadeEventosHoje: number;
  quantidadeAlarmesAtivos: number;
  quantidadeFalhasDetectadas: number;
  equipamentos: EquipamentoSaudeResponse[];
}

/** Sprint 19 (ADR 0023) — mesmos nomes do enum `PlataformaDispositivo` do backend (serializado como string). */
export type PlataformaDispositivoPush = 'Android' | 'Ios';

export interface RegistrarDispositivoPushRequest {
  propriedadeId?: string | null;
  plataforma: PlataformaDispositivoPush;
  token: string;
  modelo?: string | null;
  versaoApp?: string | null;
}

export interface AtualizarDispositivoPushRequest {
  token: string;
  versaoApp?: string | null;
}

export interface AtualizarPreferenciasDispositivoPushRequest {
  notificarAlertas: boolean;
  notificarAtividades: boolean;
  notificarGeral: boolean;
}

export interface DispositivoPushResponse {
  id: string;
  plataforma: PlataformaDispositivoPush;
  ativo: boolean;
  notificarAlertas: boolean;
  notificarAtividades: boolean;
  notificarGeral: boolean;
  ultimoUsoUtc: string;
}
