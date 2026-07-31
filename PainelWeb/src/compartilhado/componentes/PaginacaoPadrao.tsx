import { Pagination } from '@mui/material';

interface PaginacaoPadraoProps {
  paginaAtual: number;
  totalPaginas: number;
  onChange: (pagina: number) => void;
}

/** Sprint 22B (ADR 0031) — paginação padrão dos módulos administrativos, sempre server-side. */
export function PaginacaoPadrao({ paginaAtual, totalPaginas, onChange }: PaginacaoPadraoProps) {
  if (totalPaginas <= 1) return null;

  return (
    <Pagination
      count={totalPaginas}
      page={paginaAtual}
      onChange={(_, pagina) => onChange(pagina)}
      sx={{ alignSelf: 'center' }}
    />
  );
}
