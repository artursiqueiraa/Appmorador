import { Box, Paper, Typography } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { MarcaAkteck } from '../components/marca/MarcaAkteck';

/** Akteck Seg (ADR 0035) — tela de entrada com a marca e o nome do produto interno, sem decoração. */
export function AuthLayout() {
  return (
    <Box
      component="main"
      sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh', bgcolor: 'background.default', px: 2 }}
    >
      <Box sx={{ width: '100%', maxWidth: 420 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center', mb: 3 }}>
          <MarcaAkteck tamanho={36} />
        </Box>
        <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 } }}>
          <Typography variant="h2" component="h1" sx={{ mb: 0.5 }}>
            Entrar no painel
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
            Acesso da equipe Akteck Seg.
          </Typography>
          <Outlet />
        </Paper>
      </Box>
    </Box>
  );
}
