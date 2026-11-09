import type { Theme } from '@mui/material/styles';
import type { AuditoriaMasterItem, TipoAcaoAuditoria } from '../../types/api';

/**
 * Akteck Seg (ADR 0035) — vocabulário humano do Dashboard. Antes o gráfico de status
 * pintava as fatias pela ordem do array (Offline saía verde, Online laranja) e a
 * atividade recente mostrava o enum cru ("FalhaAutorizacao", "Endpoint").
 */
export const ROTULO_STATUS_EQUIPAMENTO: Record<string, string> = {
  Online: 'Online',
  Offline: 'Offline',
  Desconhecido: 'Sem informação',
};

/** Cor semântica fixa por status — nunca pela posição no gráfico. */
export function corStatusEquipamento(status: string, theme: Theme): string {
  if (status === 'Online') return theme.palette.success.main;
  if (status === 'Offline') return theme.palette.error.main;
  return theme.palette.marca.textMuted;
}

export const ORDEM_STATUS_EQUIPAMENTO = ['Online', 'Offline', 'Desconhecido'];

export { ROTULO_TIPO_PROPRIEDADE } from '../../compartilhado/tipos/propriedade';

const VERBO_ACAO: Record<TipoAcaoAuditoria, string> = {
  Login: 'entrou no painel',
  Logout: 'saiu do painel',
  ImpersonationInicio: 'começou a acessar como cliente',
  ImpersonationFim: 'saiu da conta do cliente',
  Criar: 'criou',
  Editar: 'editou',
  Excluir: 'excluiu',
  Visualizar: 'visualizou',
  FalhaAutorizacao: 'teve um acesso negado',
};

const ROTULO_ENTIDADE: Record<string, string> = {
  Propriedade: 'uma propriedade',
  Equipamento: 'um equipamento',
  Usuario: 'um usuário',
  UsuarioInterno: 'um membro da equipe',
  Provisionamento: 'um provisionamento',
  Endpoint: '',
};

/** "Carlos Henrique editou um equipamento" — nunca o nome do enum. */
export function descreverAtividade(item: AuditoriaMasterItem): string {
  const quem = item.usuarioNome && !item.usuarioNome.startsWith('(') ? item.usuarioNome : 'Usuário não identificado';
  const verbo = VERBO_ACAO[item.acao] ?? item.acao;
  const alvo = item.entidade ? (ROTULO_ENTIDADE[item.entidade] ?? '') : '';
  return [quem, verbo, alvo].filter(Boolean).join(' ');
}

export function ehAlertaDeSeguranca(item: AuditoriaMasterItem): boolean {
  return item.acao === 'FalhaAutorizacao';
}

export function formatarDataHora(utc: string): string {
  return new Date(utc).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
}
