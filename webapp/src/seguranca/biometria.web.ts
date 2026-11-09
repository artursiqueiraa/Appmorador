/**
 * Sprint 27 (ADR 0043) — no navegador não há digital/rosto: a confirmação é sempre pelo PIN e o
 * bloqueio do app não existe. Mesma interface de `biometria.ts`.
 */
export const biometria = {
  disponivel: async (): Promise<boolean> => false,
  autenticar: async (_motivo: string): Promise<boolean> => false,
  confirmarAtivo: async (): Promise<boolean> => false,
  ativarConfirmacao: async (_pin: string): Promise<void> => {},
  desativarConfirmacao: async (): Promise<void> => {},
  obterPin: async (_motivo: string): Promise<string | null> => null,
  bloqueioAtivo: async (): Promise<boolean> => false,
  definirBloqueio: async (_ativo: boolean): Promise<void> => {},
};
