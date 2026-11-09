import { useCallback, useEffect, useRef, useState } from 'react';

/**
 * Sprint 24 (ADR 0040) — "segure por N segundos" para ações que não podem disparar sem querer
 * (pânico). `iniciar` no toque, `cancelar` ao soltar: soltar antes do tempo não faz nada.
 * `progresso` vai de 0 a 1 para a animação do anel.
 */
export function useSegurarParaAcionar(duracaoMs: number, aoCompletar: () => void) {
  const [segurando, setSegurando] = useState(false);
  const [progresso, setProgresso] = useState(0);
  const inicioRef = useRef<number | null>(null);
  const intervaloRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const aoCompletarRef = useRef(aoCompletar);

  useEffect(() => {
    aoCompletarRef.current = aoCompletar;
  }, [aoCompletar]);

  const parar = useCallback(() => {
    if (intervaloRef.current) {
      clearInterval(intervaloRef.current);
      intervaloRef.current = null;
    }
    inicioRef.current = null;
    setSegurando(false);
  }, []);

  const iniciar = useCallback(() => {
    parar();
    inicioRef.current = Date.now();
    setSegurando(true);
    setProgresso(0);
    intervaloRef.current = setInterval(() => {
      if (inicioRef.current === null) {
        return;
      }
      const fracao = Math.min(1, (Date.now() - inicioRef.current) / duracaoMs);
      setProgresso(fracao);
      if (fracao >= 1) {
        parar();
        aoCompletarRef.current();
      }
    }, 50);
  }, [duracaoMs, parar]);

  const cancelar = useCallback(() => {
    if (inicioRef.current !== null) {
      parar();
      setProgresso(0);
    }
  }, [parar]);

  useEffect(() => parar, [parar]);

  return { segurando, progresso, iniciar, cancelar };
}
