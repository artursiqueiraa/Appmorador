import type { AlertaCriticoResponse } from '../api/types';

/**
 * Sprint 24 (ADR 0040) — no navegador: bipe de alarme (dois tons alternados) gerado pela própria
 * página com Web Audio, repetindo enquanto a tela de alerta estiver aberta. O navegador pode
 * bloquear o som até a primeira interação com a página; nesse caso fica só a tela cheia.
 */
let contexto: AudioContext | null = null;
let intervalo: ReturnType<typeof setInterval> | null = null;

function bipe(ctx: AudioContext, frequencia: number, inicio: number) {
  const oscilador = ctx.createOscillator();
  const ganho = ctx.createGain();
  oscilador.frequency.value = frequencia;
  ganho.gain.setValueAtTime(0.0001, inicio);
  ganho.gain.exponentialRampToValueAtTime(0.25, inicio + 0.02);
  ganho.gain.exponentialRampToValueAtTime(0.0001, inicio + 0.24);
  oscilador.connect(ganho).connect(ctx.destination);
  oscilador.start(inicio);
  oscilador.stop(inicio + 0.25);
}

export function iniciarAvisoAlerta(_alerta: AlertaCriticoResponse): void {
  try {
    contexto ??= new AudioContext();
    const ctx = contexto;
    ctx.resume().catch(() => {});
    const tocar = () => {
      const agora = ctx.currentTime;
      bipe(ctx, 880, agora);
      bipe(ctx, 660, agora + 0.3);
    };
    tocar();
    if (intervalo) {
      clearInterval(intervalo);
    }
    intervalo = setInterval(tocar, 1200);
  } catch {
    // Navegador sem Web Audio: fica só a tela cheia.
  }
}

export function pararAvisoAlerta(_alertaId: string): void {
  if (intervalo) {
    clearInterval(intervalo);
    intervalo = null;
  }
}
