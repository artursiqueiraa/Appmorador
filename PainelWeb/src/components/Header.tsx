import { useState } from 'react';
import { AppBar, Toolbar, IconButton, Menu, MenuItem, Box, Chip } from '@mui/material';
import Brightness4Icon from '@mui/icons-material/Brightness4';
import Brightness7Icon from '@mui/icons-material/Brightness7';
import AccountCircleIcon from '@mui/icons-material/AccountCircle';
import { useTemaStore } from '../stores/temaStore';
import { useAuth } from '../hooks/useAuth';
import { usePermissao } from '../hooks/usePermissao';
import { SIDEBAR_WIDTH } from './Sidebar';

export function Header() {
  const { modo, alternar } = useTemaStore();
  const { user, logout } = useAuth();
  const { roleGlobal } = usePermissao();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  return (
    <AppBar
      position="fixed"
      color="inherit"
      elevation={0}
      sx={{
        width: `calc(100% - ${SIDEBAR_WIDTH}px)`,
        ml: `${SIDEBAR_WIDTH}px`,
        borderBottom: 1,
        borderColor: 'divider',
      }}
    >
      <Toolbar sx={{ gap: 2 }}>
        <Box sx={{ flexGrow: 1 }} />
        {roleGlobal ? <Chip label={roleGlobal} size="small" color="primary" variant="outlined" /> : null}
        <IconButton onClick={alternar} aria-label="Alternar tema">
          {modo === 'dark' ? <Brightness7Icon /> : <Brightness4Icon />}
        </IconButton>
        <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} aria-label="Conta">
          <AccountCircleIcon />
        </IconButton>
        <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={() => setAnchorEl(null)}>
          <MenuItem disabled>{user?.nome}</MenuItem>
          <MenuItem onClick={() => void logout()}>Sair</MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
}
