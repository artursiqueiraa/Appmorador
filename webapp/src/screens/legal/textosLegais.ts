/**
 * Sprint 26 (ADR 0042) — Termos de Uso e Política de Privacidade da Akteck Seg.
 * Dados da empresa em "[A PREENCHER]" (decisão do usuário). Texto técnico: revisar com advogado
 * antes da venda (pendência registrada).
 */
export interface DocumentoLegal {
  titulo: string;
  versao: string;
  secoes: { titulo: string; paragrafos: string[] }[];
}

const EMPRESA = '[A PREENCHER: razão social]';
const CNPJ = '[A PREENCHER: CNPJ]';
const ENCARREGADO = '[A PREENCHER: e-mail do encarregado de dados]';
export const VERSAO_LEGAL = '2026-11-08';

export const POLITICA_PRIVACIDADE: DocumentoLegal = {
  titulo: 'Política de Privacidade',
  versao: VERSAO_LEGAL,
  secoes: [
    {
      titulo: 'Quem trata seus dados',
      paragrafos: [
        `${EMPRESA}, CNPJ ${CNPJ}, é a controladora dos dados pessoais tratados no app Akteck Seg, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).`,
        `Encarregado de dados (DPO): ${ENCARREGADO}.`,
      ],
    },
    {
      titulo: 'Quais dados usamos',
      paragrafos: [
        'Conta: nome, e-mail, telefone e senha (guardada só como hash).',
        'Propriedade: endereço, equipamentos, pessoas cadastradas (moradores, visitantes, dependentes) e veículos.',
        'Eventos de segurança: disparos, aberturas de portão, entradas nos leitores, alertas e quem os confirmou.',
        'Imagens das câmeras da propriedade, capturadas quando o alarme dispara ou quando você pede.',
        'Rosto (biometria facial) de quem usa o acesso facial — dado pessoal sensível.',
      ],
    },
    {
      titulo: 'Para que e com qual base legal',
      paragrafos: [
        'Prestar o serviço contratado: monitorar, avisar e controlar o acesso da sua propriedade (execução de contrato, art. 7º, V).',
        'Segurança da propriedade e prevenção a fraudes: histórico de eventos e de entradas (legítimo interesse, art. 7º, IX).',
        'Biometria facial: só com o consentimento específico e destacado da pessoa, ou do responsável legal no caso de menores (art. 11, I, e art. 14).',
      ],
    },
    {
      titulo: 'Com quem compartilhamos',
      paragrafos: [
        'Somente com fornecedores necessários ao serviço, sob contrato: hospedagem, envio de notificações (Google Firebase), envio de e-mail (Resend) e de SMS.',
        'O rosto é enviado apenas aos leitores faciais instalados na propriedade da pessoa.',
        'Não vendemos dados pessoais.',
      ],
    },
    {
      titulo: 'Por quanto tempo guardamos',
      paragrafos: [
        'Rosto: enquanto a pessoa estiver cadastrada e com consentimento ativo. É apagado do servidor e dos leitores assim que a pessoa ou a foto for removida, ou o consentimento revogado.',
        'Histórico de entradas e eventos de segurança, sem imagem do rosto: 6 meses após a pessoa ser removida; depois disso, o registro é anonimizado.',
        'Conta excluída: nome, e-mail, telefone e senha são apagados na hora; o histórico de segurança das propriedades permanece sem identificar você.',
      ],
    },
    {
      titulo: 'Seus direitos',
      paragrafos: [
        'Você pode confirmar o tratamento, acessar, corrigir, pedir a eliminação dos dados tratados com consentimento, revogar o consentimento e pedir informações sobre compartilhamento (art. 18).',
        'Pelo app: editar dados e excluir a conta em "Minha conta"; revogar o consentimento do rosto no cadastro da pessoa.',
        `Outros pedidos: ${ENCARREGADO}.`,
      ],
    },
    {
      titulo: 'Segurança',
      paragrafos: [
        'Comunicação criptografada, senhas com hash, credenciais de equipamentos criptografadas, acesso por papel (Proprietário, Gerente, Membro, Visualizador) e registro de auditoria das ações da equipe.',
      ],
    },
  ],
};

export const TERMOS_USO: DocumentoLegal = {
  titulo: 'Termos de Uso',
  versao: VERSAO_LEGAL,
  secoes: [
    {
      titulo: 'O serviço',
      paragrafos: [
        `O Akteck Seg, oferecido por ${EMPRESA} (CNPJ ${CNPJ}), conecta a central de alarme, câmeras e controles de acesso da sua propriedade ao celular e ao navegador.`,
        'O app avisa e ajuda a agir, mas não substitui a polícia, os bombeiros ou um serviço de monitoramento 24 horas. Em emergência, ligue 190 ou 193.',
      ],
    },
    {
      titulo: 'Sua conta',
      paragrafos: [
        'Você é responsável por manter sua senha em sigilo e por quem você convida para acessar a propriedade, e com qual papel.',
        'Cadastre o rosto de outras pessoas apenas com o consentimento delas (ou do responsável legal, no caso de menores).',
      ],
    },
    {
      titulo: 'Funcionamento',
      paragrafos: [
        'Alertas dependem de internet, energia e do funcionamento dos equipamentos e dos serviços de notificação do celular. Mantenha o app atualizado e as notificações permitidas.',
        'Comandos (armar, abrir portão) são enviados aos equipamentos instalados; a execução física depende deles.',
      ],
    },
    {
      titulo: 'Dados pessoais',
      paragrafos: ['O tratamento de dados segue a Política de Privacidade, disponível no app.'],
    },
    {
      titulo: 'Contato',
      paragrafos: [`${EMPRESA} — ${ENCARREGADO}.`],
    },
  ],
};
