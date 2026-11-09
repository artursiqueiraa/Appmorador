import type { AlertaCriticoResponse } from '../api/types';

function hora(iso: string): string {
  return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

/**
 * Sprint 24 (ADR 0040) — uma frase sobre quem já foi avisado e o que acontece se ninguém
 * confirmar. Linguagem do dono, nunca "nível" ou "escalonamento".
 */
export function situacaoEscalonamento(alerta: AlertaCriticoResponse): string {
  if (alerta.confirmado) {
    return `Confirmado por ${alerta.confirmadoPorNome ?? 'alguém da casa'}${alerta.confirmadoEmUtc ? ` às ${hora(alerta.confirmadoEmUtc)}` : ''}.`;
  }
  if (alerta.escalonadoContatosEmUtc) {
    return `Ninguém confirmou ainda. Seus contatos de emergência receberam um SMS às ${hora(alerta.escalonadoContatosEmUtc)}.`;
  }
  if (alerta.escalonadoMembrosEmUtc) {
    return alerta.proximoEscalonamentoEmUtc
      ? `As outras pessoas da casa foram avisadas. Se ninguém confirmar até ${hora(alerta.proximoEscalonamentoEmUtc)}, seus contatos de emergência recebem um SMS.`
      : 'As outras pessoas da casa foram avisadas.';
  }
  if (alerta.proximoEscalonamentoEmUtc) {
    return `Se ninguém confirmar até ${hora(alerta.proximoEscalonamentoEmUtc)}, avisaremos as próximas pessoas da sua lista.`;
  }
  return 'Toque em "Estou ciente" quando verificar o que aconteceu.';
}
