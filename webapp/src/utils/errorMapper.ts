export interface UserError {
  titulo: string;
  mensagem: string;
  podeTentarNovamente: boolean;
}

interface ClassificarErroInput {
  /** `undefined` = falha de rede (nenhuma resposta chegou a existir). */
  status?: number;
  mensagemTecnica: string;
  temConexaoInternet: boolean;
}

/**
 * Sprint 17 (ADR 0020) — nunca deixa o morador ver stack trace, status HTTP ou texto
 * de exceção .NET. Backend não expõe um código de erro estruturado (mudar isso seria
 * alterar contrato de API, fora do escopo desta Sprint) — a classificação é
 * heurística, baseada em status HTTP + padrões de texto já usados pelos Providers
 * (`JflProvider`/`IntelbrasProvider`/`ControlIdProvider`: "não possui sessão ativa",
 * "não foi possível conectar a...: {ex.Message}", "não respondeu a tempo"). Mensagens
 * de validação de domínio já bem escritas em pt-BR (ex.: "Nome é obrigatório.") não
 * são técnicas — passam direto, a menos que contenham um termo da Regra de
 * Vocabulário ou um marcador de vazamento técnico.
 */
const MARCADORES_VAZAMENTO_TECNICO = [
  'exception',
  'stacktrace',
  'system.',
  'httprequestexception',
  'taskcanceledexception',
  'socketexception',
  'timeoutexception',
  'unable to',
  'connection refused',
  'no route to host',
];

const TERMOS_PROIBIDOS = [
  'intelbras',
  'jfl',
  'control id',
  'pgm',
  'zona',
  'partic',
  'snapshot',
  'signalr',
  'provider',
  'equipamento',
  'tcp',
  'http',
  'protocolo',
  'handshake',
  'keep alive',
  'httpclient',
  'timeout',
  'request',
];

const PADROES_DISPOSITIVO_INDISPONIVEL = [
  'não foi possível conectar a',
  'não possui sessão ativa',
  'não possui conexão ativa',
  'não respondeu',
  'nao respondeu',
  'tempo esgotado',
  'não respondeu a tempo',
  '(offline)',
];

function contemAlgum(texto: string, marcadores: string[]): boolean {
  const alvo = texto.toLowerCase();
  return marcadores.some((marcador) => alvo.includes(marcador));
}

export function mapErrorToUserMessage({ status, mensagemTecnica, temConexaoInternet }: ClassificarErroInput): UserError {
  if (!temConexaoInternet) {
    return { titulo: 'Sem conexão', mensagem: 'Verifique sua conexão com a internet e tente novamente.', podeTentarNovamente: true };
  }

  if (status === 401 || status === 403) {
    return { titulo: 'Sem permissão', mensagem: 'Você não tem permissão para isso. Entre em contato com o administrador.', podeTentarNovamente: false };
  }

  if (status === undefined || status >= 500) {
    return {
      titulo: 'Servidor indisponível',
      mensagem: 'O servidor está temporariamente indisponível. Tente novamente em alguns instantes.',
      podeTentarNovamente: true,
    };
  }

  if (contemAlgum(mensagemTecnica, PADROES_DISPOSITIVO_INDISPONIVEL)) {
    return { titulo: 'Dispositivo não responde', mensagem: 'O dispositivo não respondeu. Verifique se ele está ligado e conectado.', podeTentarNovamente: true };
  }

  if (status === 404) {
    return { titulo: 'Não encontrado', mensagem: 'Não encontramos o que você procura.', podeTentarNovamente: false };
  }

  if (contemAlgum(mensagemTecnica, MARCADORES_VAZAMENTO_TECNICO) || contemAlgum(mensagemTecnica, TERMOS_PROIBIDOS)) {
    return { titulo: 'Algo deu errado', mensagem: 'Algo deu errado. Tente novamente ou entre em contato com o suporte.', podeTentarNovamente: true };
  }

  // Mensagem de validação de domínio já em pt-BR e sem termo técnico — segura para mostrar.
  return { titulo: 'Não foi possível continuar', mensagem: mensagemTecnica, podeTentarNovamente: false };
}
