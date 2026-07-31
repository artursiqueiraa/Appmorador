import { create } from 'zustand';

export type ToastSeveridade = 'success' | 'error' | 'warning' | 'info';

interface ToastState {
  aberto: boolean;
  mensagem: string;
  severidade: ToastSeveridade;
  mostrar: (mensagem: string, severidade?: ToastSeveridade) => void;
  fechar: () => void;
}

/** Sprint 22A (Fase 7) — feedback visual único no app: toast de sucesso/erro/aviso, desaparece sozinho em 5s. */
export const useToastStore = create<ToastState>((set) => ({
  aberto: false,
  mensagem: '',
  severidade: 'info',
  mostrar: (mensagem, severidade = 'info') => set({ aberto: true, mensagem, severidade }),
  fechar: () => set({ aberto: false }),
}));
