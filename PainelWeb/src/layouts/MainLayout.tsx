import { useState } from 'react';
import { Box, useMediaQuery } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { Sidebar } from '../components/Sidebar';
import { Header } from '../components/Header';
import { ImpersonationBanner } from '../components/ImpersonationBanner';
import { useAuthStore } from '../stores/authStore';
import { MobileNotSupportedPage } from '../pages/MobileNotSupportedPage';
import { layout } from '../styles/tokens';

const ALTURA_BANNER = 48;

/**
 * Sprint 22A (Fase 1) — desktop principal, tablet secundário, mobile não suportado.
 * Akteck Seg (ADR 0035):
 * - corrige o conteúdo deslocado 480px: o Drawer permanente já ocupa a largura dele no
 *   fluxo flex, e o `ml` extra no <main> somava mais 240px;
 * - abaixo de 960px o menu vira gaveta temporária;
 * - conteúdo com largura máxima, para tabelas e gráficos não esticarem em 1920px.
 */
export function MainLayout() {
  const telaPequena = useMediaQuery('(max-width:599px)');
  const menuRecolhido = useMediaQuery('(max-width:959px)');
  const [menuAberto, setMenuAberto] = useState(false);
  const impersonando = useAuthStore((s) => Boolean(s.impersonation));

  const topo = impersonando ? ALTURA_BANNER : 0;

  // Em tela pequena o aviso cobre o painel, mas o painel continua montado (só escondido):
  // estreitar a janela por um instante nunca apaga um formulário pela metade.
  return (
    <>
      {telaPequena ? <MobileNotSupportedPage /> : null}
      <Box sx={{ display: telaPequena ? 'none' : 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
        {impersonando ? <ImpersonationBanner /> : null}
        <Sidebar temporario={menuRecolhido} aberto={menuAberto} onFechar={() => setMenuAberto(false)} />
        <Header
          deslocamento={menuRecolhido ? 0 : layout.sidebarWidth}
          topo={topo}
          onAbrirMenu={menuRecolhido ? () => setMenuAberto(true) : undefined}
        />
        <Box component="main" sx={{ flexGrow: 1, minWidth: 0, pt: `${layout.headerHeight + topo}px` }}>
          <Box sx={{ maxWidth: layout.contentMaxWidth, mx: 'auto', px: { sm: 3, lg: 4 }, py: 3 }}>
            <Outlet />
          </Box>
        </Box>
      </Box>
    </>
  );
}
