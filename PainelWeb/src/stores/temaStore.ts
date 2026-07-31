import { create } from 'zustand';

type Modo = 'light' | 'dark';

const CHAVE_TEMA = 'painel.tema';

function lerTemaInicial(): Modo {
  const salvo = localStorage.getItem(CHAVE_TEMA);
  if (salvo === 'light' || salvo === 'dark') return salvo;
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

interface TemaState {
  modo: Modo;
  alternar: () => void;
}

export const useTemaStore = create<TemaState>((set) => ({
  modo: lerTemaInicial(),
  alternar: () =>
    set((state) => {
      const novoModo: Modo = state.modo === 'light' ? 'dark' : 'light';
      localStorage.setItem(CHAVE_TEMA, novoModo);
      return { modo: novoModo };
    }),
}));
