/**
 * Sprint 18 (ADR 0022) — telemetria de desenvolvimento/observabilidade, nunca
 * exposta ao morador (só `console.info` em `__DEV__`, nenhuma métrica técnica
 * aparece em nenhuma tela). Serve para diagnosticar o comportamento do
 * realtime durante o desenvolvimento (conexão, reconexão, snapshots, comandos)
 * sem precisar instrumentar cada tela manualmente.
 */
type EventoTelemetria =
  | { tipo: 'signalr_conectado'; propriedadeId: string | null; tentativa: number }
  | { tipo: 'signalr_desconectado'; motivo: string }
  | { tipo: 'signalr_reconectando'; tentativa: number; intervaloMs: number }
  | { tipo: 'signalr_reconectado' }
  | { tipo: 'signalr_sem_comunicacao'; tentativas: number }
  | { tipo: 'snapshot_recebido'; propriedadeId: string; motivo: string }
  | { tipo: 'evento_processado'; eventoId: string; msDesdeRecebido: number }
  | { tipo: 'comando_enviado'; comando: string; equipamentoId: string }
  | { tipo: 'comando_resultado'; comando: string; sucesso: boolean; msResposta: number }
  | { tipo: 'cache_hit'; componente: string }
  | { tipo: 'cache_miss'; componente: string };

export function registrarTelemetria(evento: EventoTelemetria): void {
  if (!__DEV__) {
    return;
  }

  console.info(`[telemetria] ${evento.tipo}`, evento);
}
