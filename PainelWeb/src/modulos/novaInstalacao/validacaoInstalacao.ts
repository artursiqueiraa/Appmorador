import type { TipoPropriedade } from '../../compartilhado/tipos/propriedade';

export interface DadosInstalacao {
  nomeCliente: string;
  emailCliente: string;
  senhaInicial: string;
  nomePropriedade: string;
  tipoPropriedade: TipoPropriedade | '';
  enderecoPropriedade: string;
  equipamentoId: string;
}

export const REGRA_SENHA = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;
export const REGRA_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function etapaClienteValida(d: DadosInstalacao) {
  return d.nomeCliente.trim().length > 0 && REGRA_EMAIL.test(d.emailCliente.trim()) && REGRA_SENHA.test(d.senhaInicial);
}

export function etapaPropriedadeValida(d: DadosInstalacao) {
  return d.nomePropriedade.trim().length > 0 && d.tipoPropriedade !== '';
}

export type Alterar = <K extends keyof DadosInstalacao>(campo: K, valor: DadosInstalacao[K]) => void;
