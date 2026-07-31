import type { ReactNode } from 'react';
import {
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  TableContainer,
  Paper,
  Skeleton,
} from '@mui/material';
import { EmptyState } from '../../components/EmptyState';
import type { SvgIconComponent } from '@mui/icons-material';

export interface ColunaTabela<T> {
  cabecalho: string;
  render: (item: T) => ReactNode;
  /** Alinhamento da célula — padrão `left`. */
  align?: 'left' | 'center' | 'right';
}

interface TabelaPadraoProps<T> {
  colunas: ColunaTabela<T>[];
  itens: T[];
  chave: (item: T) => string;
  carregando?: boolean;
  onRowClick?: (item: T) => void;
  vazio: { icone: SvgIconComponent; titulo: string; descricao: string };
}

/**
 * Sprint 22B (ADR 0031) — tabela genérica por colunas, evitando que cada tela do Painel Web
 * reescreva `Table/TableHead/TableBody` do zero (ver `ClientesListPage`, que fazia isso antes
 * deste módulo existir). Usada pelos 3 módulos novos desta Sprint.
 */
export function TabelaPadrao<T>({ colunas, itens, chave, carregando, onRowClick, vazio }: TabelaPadraoProps<T>) {
  if (carregando) {
    return <Skeleton variant="rounded" height={400} />;
  }

  if (itens.length === 0) {
    return (
      <Paper variant="outlined">
        <EmptyState icone={vazio.icone} titulo={vazio.titulo} descricao={vazio.descricao} />
      </Paper>
    );
  }

  return (
    <TableContainer component={Paper} variant="outlined">
      <Table>
        <TableHead>
          <TableRow>
            {colunas.map((coluna) => (
              <TableCell key={coluna.cabecalho} align={coluna.align}>
                {coluna.cabecalho}
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {itens.map((item) => (
            <TableRow
              key={chave(item)}
              hover={Boolean(onRowClick)}
              sx={onRowClick ? { cursor: 'pointer' } : undefined}
              onClick={onRowClick ? () => onRowClick(item) : undefined}
            >
              {colunas.map((coluna) => (
                <TableCell key={coluna.cabecalho} align={coluna.align}>
                  {coluna.render(item)}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
