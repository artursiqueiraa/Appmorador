/** Sprint 22B (ADR 0031) — formato de paginação usado por todo endpoint novo dos módulos administrativos. */
export interface PaginadoResponse<T> {
  itens: T[];
  paginaAtual: number;
  totalPaginas: number;
  totalItens: number;
}
