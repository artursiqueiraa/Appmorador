import { Box, Button, Typography } from '@mui/material';
import type { SvgIconComponent } from '@mui/icons-material';

interface EmptyStateProps {
  icone: SvgIconComponent;
  titulo: string;
  descricao: string;
  acao?: { rotulo: string; onClick: () => void };
}

/** Sprint 22A (Fase 7) — nenhuma lista fica em branco sem explicação: ícone + mensagem amigável + CTA opcional. */
export function EmptyState({ icone: Icone, titulo, descricao, acao }: EmptyStateProps) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 1.5,
        py: 6,
        px: 2,
        textAlign: 'center',
      }}
    >
      <Icone sx={{ fontSize: 48, color: 'text.secondary' }} />
      <Typography variant="h3">{titulo}</Typography>
      <Typography variant="body1" sx={{ color: 'text.secondary', maxWidth: 360 }}>
        {descricao}
      </Typography>
      {acao ? (
        <Button variant="outlined" onClick={acao.onClick} sx={{ mt: 1 }}>
          {acao.rotulo}
        </Button>
      ) : null}
    </Box>
  );
}
