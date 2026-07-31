import { Breadcrumbs as MuiBreadcrumbs, Link, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';

export interface BreadcrumbItem {
  rotulo: string;
  rota?: string;
}

/** Sprint 22A (Fase 7) — navegação hierárquica em toda tela interna. */
export function Breadcrumbs({ itens }: { itens: BreadcrumbItem[] }) {
  return (
    <MuiBreadcrumbs sx={{ mb: 2 }}>
      {itens.map((item, indice) =>
        item.rota && indice < itens.length - 1 ? (
          <Link key={item.rotulo} component={RouterLink} to={item.rota} underline="hover" color="inherit">
            {item.rotulo}
          </Link>
        ) : (
          <Typography key={item.rotulo} color="text.primary">
            {item.rotulo}
          </Typography>
        ),
      )}
    </MuiBreadcrumbs>
  );
}
