import { Box, Paper, Typography } from '@mui/material';
import { Outlet } from 'react-router-dom';

export function AuthLayout() {
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '100vh',
        bgcolor: 'background.default',
      }}
    >
      <Paper elevation={0} sx={{ p: 5, width: 400, border: 1, borderColor: 'divider' }}>
        <Typography variant="h1" sx={{ textAlign: 'center', mb: 1, fontWeight: 800, color: 'primary.main' }}>
          AppMorador
        </Typography>
        <Typography variant="body1" sx={{ textAlign: 'center', color: 'text.secondary', mb: 4 }}>
          Painel Administrativo
        </Typography>
        <Outlet />
      </Paper>
    </Box>
  );
}
