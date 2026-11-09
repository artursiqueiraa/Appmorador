import { Breadcrumbs as MuiBreadcrumbs, Link, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';

export interface BreadcrumbItem {
  rotulo: string;
  rota?: string;
}

/** Sprint 22A (Fase 7) — navegação hierárquica em toda tela interna. */
/**
 * Akteck Seg (ADR 0035) — o cabeçalho já mostra "Grupo / Página". Aqui a trilha só aparece em
 * telas de detalhe (dois níveis ou mais) e sem a raiz "Dashboard", que repetia o menu.
 */
export function Breadcrumbs({ itens: todos }: { itens: BreadcrumbItem[] }) {
  const itens = todos.filter((i) => i.rotulo !== 'Dashboard');
  if (itens.length <= 1) {
    return null;
  }
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
