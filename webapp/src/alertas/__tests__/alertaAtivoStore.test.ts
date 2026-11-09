/**
 * Sprint 24 (ADR 0040) — regras de quando a tela cheia de alerta crítico aparece, some ou fica
 * escondida neste aparelho.
 */
import type { AlertaCriticoResponse } from '../../api/types';
import { alertaAtivoStore } from '../alertaAtivoStore';

function alerta(parcial: Partial<AlertaCriticoResponse> = {}): AlertaCriticoResponse {
  return {
    id: 'a1',
    propriedadeId: 'p1',
    tipo: 'Disparo',
    titulo: 'Alarme disparado',
    descricao: 'Movimento detectado — Sala',
    criadoEmUtc: '2026-11-08T12:00:00Z',
    confirmado: false,
    nivelEscalonamento: 'Dono',
    acionadoPeloApp: false,
    ...parcial,
  };
}

describe('alertaAtivoStore', () => {
  beforeEach(() => alertaAtivoStore.limpar());

  it('alerta aberto recebido aparece', () => {
    alertaAtivoStore.receber(alerta());
    expect(alertaAtivoStore.obter()?.id).toBe('a1');
  });

  it('confirmacao do mesmo alerta (por qualquer pessoa) fecha a tela', () => {
    alertaAtivoStore.receber(alerta());
    alertaAtivoStore.receber(alerta({ confirmado: true }));
    expect(alertaAtivoStore.obter()).toBeNull();
  });

  it('confirmacao de outro alerta nao fecha o que esta aberto', () => {
    alertaAtivoStore.receber(alerta());
    alertaAtivoStore.receber(alerta({ id: 'a2', confirmado: true }));
    expect(alertaAtivoStore.obter()?.id).toBe('a1');
  });

  it('dispensado neste aparelho nao reabre com uma atualizacao (escalonou)', () => {
    alertaAtivoStore.receber(alerta());
    alertaAtivoStore.dispensar();
    alertaAtivoStore.receber(alerta({ nivelEscalonamento: 'ContatosEmergencia' }));
    expect(alertaAtivoStore.obter()).toBeNull();
  });

  it('tocar na notificacao reabre mesmo depois de dispensado', () => {
    alertaAtivoStore.receber(alerta());
    alertaAtivoStore.dispensar();
    alertaAtivoStore.abrirExplicitamente(alerta());
    expect(alertaAtivoStore.obter()?.id).toBe('a1');
  });

  it('avisa quem assina a cada mudanca', () => {
    const ouvinte = jest.fn();
    const cancelar = alertaAtivoStore.assinar(ouvinte);
    alertaAtivoStore.receber(alerta());
    alertaAtivoStore.dispensar();
    cancelar();
    alertaAtivoStore.receber(alerta({ id: 'a3' }));
    expect(ouvinte).toHaveBeenCalledTimes(2);
  });
});
