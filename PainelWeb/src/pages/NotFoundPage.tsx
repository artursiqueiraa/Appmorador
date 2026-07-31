import { Box, Button, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';

export function NotFoundPage() {
  const navigate = useNavigate();
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2, py: 10 }}>
      <Typography variant="h1">404</Typography>
      <Typography variant="body1" color="text.secondary">
        Página não encontrada.
      </Typography>
      <Button variant="contained" onClick={() => navigate('/dashboard')}>
        Voltar ao Dashboard
      </Button>
    </Box>
  );
}
