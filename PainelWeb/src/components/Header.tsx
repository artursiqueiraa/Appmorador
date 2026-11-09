import { useState } from 'react';
import { useLocation } from 'react-router-dom';
import { AppBar, Avatar, Box, Chip, Divider, IconButton, ListItemIcon, Menu, MenuItem, Toolbar, Tooltip, Typography } from '@mui/material';
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined';
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined';
import MenuIcon from '@mui/icons-material/Menu';
import { useTemaStore } from '../stores/temaStore';
import { useAuth } from '../hooks/useAuth';
import { usePermissao } from '../hooks/usePermissao';
import { localizarNaNavegacao } from '../routes/navegacao';
import { layout } from '../styles/tokens';

const ROTULO_PAPEL: Record<string, string> = { Master: 'Master', Suporte: 'Suporte', Tecnico: 'Técnico' };

interface HeaderProps {
  deslocamento: number;
  topo: number;
  onAbrirMenu?: () => void;
}

/** Akteck Seg (ADR 0035) — cabeçalho com a trilha "Grupo / Página" (antes vazio), papel, tema e conta. */
export function Header({ deslocamento, topo, onAbrirMenu }: HeaderProps) {
  const { modo, alternar } = useTemaStore();
  const { user, logout } = useAuth();
  const { roleGlobal } = usePermissao();
  const { pathname } = useLocation();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const local = localizarNaNavegacao(pathname);
  const iniciais = (user?.nome ?? '?')
    .split(' ')
    .slice(0, 2)
    .map((p) => p[0])
    .join('')
    .toUpperCase();

  return (
    <AppBar
      position="fixed"
      color="inherit"
      elevation={0}
      sx={{ width: `calc(100% - ${deslocamento}px)`, ml: `${deslocamento}px`, top: topo, borderBottom: 1, borderColor: 'divider' }}
    >
      <Toolbar sx={{ gap: 1.5, minHeight: `${layout.headerHeight}px !important` }}>
        {onAbrirMenu ? (
          <IconButton onClick={onAbrirMenu} aria-label="Abrir menu" edge="start">
            <MenuIcon />
          </IconButton>
        ) : null}
        <Box sx={{ flexGrow: 1, minWidth: 0 }}>
          {local ? (
            <Typography component="p" noWrap sx={{ fontSize: 14 }}>
              {local.grupo.rotulo !== local.item.rotulo ? (
                <>
                  <Box component="span" sx={{ color: 'text.secondary' }}>
                    {local.grupo.rotulo}
                  </Box>
                  <Box component="span" sx={{ color: 'text.secondary', mx: 1 }} aria-hidden="true">
                    /
                  </Box>
                </>
              ) : null}
              <Box component="span" sx={{ color: 'text.primary', fontWeight: 600 }}>
                {local.item.rotulo}
              </Box>
            </Typography>
          ) : null}
        </Box>
        {roleGlobal ? <Chip label={ROTULO_PAPEL[roleGlobal] ?? roleGlobal} size="small" variant="outlined" /> : null}
        <Tooltip title={modo === 'dark' ? 'Usar tema claro' : 'Usar tema escuro'}>
          <IconButton onClick={alternar} aria-label={modo === 'dark' ? 'Usar tema claro' : 'Usar tema escuro'}>
            {modo === 'dark' ? <LightModeOutlinedIcon fontSize="small" /> : <DarkModeOutlinedIcon fontSize="small" />}
          </IconButton>
        </Tooltip>
        <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} aria-label="Conta" aria-haspopup="menu">
          <Avatar sx={{ width: 30, height: 30, fontSize: 12, fontWeight: 600, bgcolor: 'primary.main', color: 'primary.contrastText' }}>
            {iniciais}
          </Avatar>
        </IconButton>
        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
          transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        >
          <Box sx={{ px: 2, py: 1 }}>
            <Typography sx={{ fontWeight: 600 }}>{user?.nome}</Typography>
            <Typography variant="caption" color="text.secondary">
              {user?.email}
            </Typography>
          </Box>
          <Divider />
          <MenuItem onClick={() => void logout()}>
            <ListItemIcon>
              <LogoutOutlinedIcon fontSize="small" />
            </ListItemIcon>
            Sair
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
}
