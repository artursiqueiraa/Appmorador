import { Box, Typography } from '@mui/material';
import type { ReactNode } from 'react';
import { Breadcrumbs, type BreadcrumbItem } from '../../components/Breadcrumbs';

interface CabecalhoPaginaProps {
  titulo: string;
  breadcrumbs: BreadcrumbItem[];
  acao?: ReactNode;
}

/**
 * Sprint 22B (ADR 0031) — consolida o padrão `<Breadcrumbs/><Typography variant="h1">` repetido
 * em toda página do módulo (ex.: `ClientesListPage`), com um slot opcional para uma ação principal
 * (ex.: botão "Novo Equipamento") alinhada à direita.
 */
export function CabecalhoPagina({ titulo, breadcrumbs, acao }: CabecalhoPaginaProps) {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
      <Breadcrumbs itens={breadcrumbs} />
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
        <Typography variant="h1">{titulo}</Typography>
        {acao}
      </Box>
    </Box>
  );
}
