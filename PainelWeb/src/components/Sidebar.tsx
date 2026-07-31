import { useMemo } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Divider,
  Box,
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import PeopleIcon from '@mui/icons-material/People';
import SupportAgentIcon from '@mui/icons-material/SupportAgent';
import HistoryIcon from '@mui/icons-material/History';
import DescriptionIcon from '@mui/icons-material/Description';
import DevicesIcon from '@mui/icons-material/Devices';
import Inventory2Icon from '@mui/icons-material/Inventory2';
import MonitorHeartIcon from '@mui/icons-material/MonitorHeart';
import { usePermissao } from '../hooks/usePermissao';

export const SIDEBAR_WIDTH = 240;

interface ItemMenu {
  rotulo: string;
  rota: string;
  icone: typeof DashboardIcon;
  /** Se ausente, visível para qualquer interno. */
  visivelPara?: () => boolean;
}

export function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isMaster, isTecnico, isSuporte, podeVerTudo, podeGerenciarHardware } = usePermissao();

  const itens: ItemMenu[] = useMemo(
    () => [
      {
        rotulo: 'Dashboard',
        rota: isTecnico && !isMaster && !isSuporte ? '/dashboard-tecnico' : '/dashboard',
        icone: DashboardIcon,
      },
      { rotulo: 'Clientes', rota: '/clientes', icone: PeopleIcon, visivelPara: () => podeVerTudo },
      {
        rotulo: 'Equipamentos',
        rota: '/equipamentos',
        icone: DevicesIcon,
        visivelPara: () => podeGerenciarHardware,
      },
      {
        rotulo: 'Provisionamentos',
        rota: '/provisionamentos',
        icone: Inventory2Icon,
        visivelPara: () => podeGerenciarHardware,
      },
      {
        rotulo: 'Diagnóstico de Equipamentos',
        rota: '/diagnostico-equipamentos',
        icone: MonitorHeartIcon,
        visivelPara: () => podeGerenciarHardware,
      },
      {
        rotulo: 'Suporte',
        rota: '/suporte/selecionar-cliente',
        icone: SupportAgentIcon,
        visivelPara: () => podeVerTudo,
      },
      { rotulo: 'Sessões Ativas', rota: '/suporte/sessoes-ativas', icone: HistoryIcon, visivelPara: () => podeVerTudo },
      { rotulo: 'Logs', rota: '/suporte/logs', icone: DescriptionIcon, visivelPara: () => podeVerTudo },
    ],
    [isTecnico, isMaster, isSuporte, podeVerTudo, podeGerenciarHardware],
  );

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: SIDEBAR_WIDTH,
        flexShrink: 0,
        [`& .MuiDrawer-paper`]: { width: SIDEBAR_WIDTH, boxSizing: 'border-box' },
      }}
    >
      <Toolbar>
        <Typography variant="h3" noWrap sx={{ fontWeight: 800 }}>
          AppMorador
        </Typography>
      </Toolbar>
      <Divider />
      <Box sx={{ flexGrow: 1 }}>
        <List>
          {itens
            .filter((item) => !item.visivelPara || item.visivelPara())
            .map((item) => {
              const Icone = item.icone;
              const ativo = location.pathname.startsWith(item.rota.split('/').slice(0, 2).join('/'));
              return (
                <ListItemButton key={item.rota} selected={ativo} onClick={() => navigate(item.rota)}>
                  <ListItemIcon>
                    <Icone color={ativo ? 'primary' : undefined} />
                  </ListItemIcon>
                  <ListItemText primary={item.rotulo} />
                </ListItemButton>
              );
            })}
        </List>
      </Box>
    </Drawer>
  );
}
