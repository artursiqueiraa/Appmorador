import { Box, Typography } from '@mui/material';
import DesktopWindowsIcon from '@mui/icons-material/DesktopWindows';

/** Sprint 22A (Fase 1) — mobile não é suportado no Painel Web; use o app AppMorador no celular. */
export function MobileNotSupportedPage() {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '100vh',
        gap: 2,
        px: 4,
        textAlign: 'center',
        bgcolor: 'background.default',
      }}
    >
      <DesktopWindowsIcon sx={{ fontSize: 56, color: 'primary.main' }} />
      <Typography variant="h2">Use um computador ou tablet</Typography>
      <Typography variant="body1" sx={{ color: 'text.secondary', maxWidth: 360 }}>
        O Painel Administrativo do AppMorador foi feito para telas maiores. No celular, use o app AppMorador.
      </Typography>
    </Box>
  );
}
