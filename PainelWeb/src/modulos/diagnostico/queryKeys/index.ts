export const diagnosticoKeys = {
  all: ['diagnostico'] as const,
  status: (pagina: number, tamanhoPagina: number) => [...diagnosticoKeys.all, 'status', pagina, tamanhoPagina] as const,
};
