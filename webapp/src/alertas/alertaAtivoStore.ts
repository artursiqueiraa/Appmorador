import { useSyncExternalStore } from 'react';
import type { AlertaCriticoResponse } from '../api/types';

/**
 * Sprint 24 (ADR 0040) — qual alerta crítico a tela cheia está mostrando. Três fontes alimentam
 * este store: o tempo real (alerta novo/confirmado/escalonado), o toque na notificação push e a
 * checagem de alertas abertos ao entrar numa propriedade. Fica fora de React Context para o
 * handler de notificação (fora da árvore) conseguir abrir o alerta.
 *
 * Regras:
 * - alerta confirmado (por qualquer pessoa) fecha a tela se for o que está aberto;
 * - "dispensar" esconde só neste aparelho, sem confirmar — o alerta continua escalonando;
 *   uma atualização posterior do mesmo alerta (ex.: escalonou) não reabre o que foi dispensado.
 */
let atual: AlertaCriticoResponse | null = null;
const dispensados = new Set<string>();
const ouvintes = new Set<() => void>();

function emitir() {
  ouvintes.forEach((ouvir) => ouvir());
}

export const alertaAtivoStore = {
  obter: () => atual,

  /** Recebeu um alerta (tempo real, push ou checagem). Decide se mostra, atualiza ou fecha. */
  receber(alerta: AlertaCriticoResponse) {
    if (alerta.confirmado) {
      if (atual?.id === alerta.id) {
        atual = null;
        emitir();
      }
      return;
    }
    if (dispensados.has(alerta.id)) {
      return;
    }
    atual = alerta;
    emitir();
  },

  /** Abriu pela notificação: mostra mesmo que tenha sido dispensado antes (foi um pedido explícito). */
  abrirExplicitamente(alerta: AlertaCriticoResponse) {
    dispensados.delete(alerta.id);
    alertaAtivoStore.receber(alerta);
  },

  dispensar() {
    if (atual) {
      dispensados.add(atual.id);
      atual = null;
      emitir();
    }
  },

  /** Troca de propriedade ou logout: nada da propriedade anterior pode continuar na tela. */
  limpar() {
    atual = null;
    dispensados.clear();
    emitir();
  },

  assinar(ouvir: () => void) {
    ouvintes.add(ouvir);
    return () => {
      ouvintes.delete(ouvir);
    };
  },
};

export function useAlertaAtivo(): AlertaCriticoResponse | null {
  return useSyncExternalStore(alertaAtivoStore.assinar, alertaAtivoStore.obter, alertaAtivoStore.obter);
}
