import { useEffect, useState } from 'react';

/** Sprint 22B (ADR 0031) — versão compartilhada do debounce já usado (duplicado) em `ClientesListPage`. */
export function useDebounce<T>(valor: T, atrasoMs: number): T {
  const [debounced, setDebounced] = useState(valor);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(valor), atrasoMs);
    return () => clearTimeout(timer);
  }, [valor, atrasoMs]);

  return debounced;
}
