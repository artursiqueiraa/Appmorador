import type { PerfilPropriedade } from '../api/types';

/** Sprint 25 (ADR 0041) — nome e explicação de cada papel, na linguagem do dono. */
export const PAPEIS: Record<PerfilPropriedade, { rotulo: string; descricao: string }> = {
  Administrador: { rotulo: 'Proprietário', descricao: 'Pode tudo, inclusive convidar e remover pessoas.' },
  Gerente: { rotulo: 'Gerente', descricao: 'Pode tudo, menos gerenciar pessoas.' },
  Morador: { rotulo: 'Membro', descricao: 'Vê, arma e desarma, abre portões e aciona o pânico.' },
  Visualizador: { rotulo: 'Visualizador', descricao: 'Só vê câmeras, histórico e status, e confirma alertas.' },
};

export const PAPEIS_CONVIDAVEIS: PerfilPropriedade[] = ['Gerente', 'Morador', 'Visualizador'];

/** O que cada papel pode fazer no app (espelha a tabela do backend, AcessoPropriedadeServico). */
export function podeOperar(perfil: PerfilPropriedade | null | undefined): boolean {
  return perfil !== 'Visualizador';
}

export function podeGerenciarPessoas(perfil: PerfilPropriedade | null | undefined): boolean {
  return perfil === 'Administrador';
}
