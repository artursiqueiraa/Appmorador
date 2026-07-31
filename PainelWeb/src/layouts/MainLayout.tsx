import { Box, Toolbar, useMediaQuery } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { Sidebar, SIDEBAR_WIDTH } from '../components/Sidebar';
import { Header } from '../components/Header';
import { ImpersonationBanner } from '../components/ImpersonationBanner';
import { useAuthStore } from '../stores/authStore';
import { MobileNotSupportedPage } from '../pages/MobileNotSupportedPage';

/**
 * Sprint 22A (Fase 1) — desktop principal, tablet secundário, mobile não suportado (mensagem
 * amigável em vez de uma UI quebrada tentando caber numa tela pequena demais).
 */
export function MainLayout() {
  const telaPequena = useMediaQuery('(max-width:599px)');
  const impersonando = useAuthStore((s) => Boolean(s.impersonation));

  if (telaPequena) {
    return <MobileNotSupportedPage />;
  }

  return (
    <Box sx={{ display: 'flex' }}>
      {impersonando ? <ImpersonationBanner /> : null}
      <Sidebar />
      <Header />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          ml: `${SIDEBAR_WIDTH}px`,
          mt: impersonando ? '48px' : 0,
          minHeight: '100vh',
          bgcolor: 'background.default',
        }}
      >
        <Toolbar />
        <Box sx={{ p: 3 }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
}
