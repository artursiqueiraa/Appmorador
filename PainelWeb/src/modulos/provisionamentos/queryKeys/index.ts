export const provisionamentosKeys = {
  all: ['provisionamentos'] as const,
  ativos: (pagina: number, tamanhoPagina: number) => [...provisionamentosKeys.all, 'ativos', pagina, tamanhoPagina] as const,
  dashboard: () => [...provisionamentosKeys.all, 'dashboard'] as const,
  historico: (equipamentoId: string) => [...provisionamentosKeys.all, 'historico', equipamentoId] as const,
};
