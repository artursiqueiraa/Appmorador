import { alertaAtivoStore } from './alertaAtivoStore';
import { servicoAlertas } from './servicoAlertas';

/**
 * Sprint 24 (ADR 0040) — toque numa notificação crítica (acao "ABRIR_ALERTA"): busca o alerta e
 * abre a tela cheia. Com o app abrindo a frio, a sessão pode ainda não estar pronta — tenta de
 * novo algumas vezes (até ~5 s) antes de desistir em silêncio.
 */
export function abrirAlertaPorNotificacao(alertaId: unknown, tentativasRestantes = 10): void {
  if (typeof alertaId !== 'string' || alertaId.length === 0) {
    return;
  }

  servicoAlertas
    .obter(alertaId)
    .then((alerta) => alertaAtivoStore.abrirExplicitamente(alerta))
    .catch(() => {
      if (tentativasRestantes > 0) {
        setTimeout(() => abrirAlertaPorNotificacao(alertaId, tentativasRestantes - 1), 500);
      }
    });
}
