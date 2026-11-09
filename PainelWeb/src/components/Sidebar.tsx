import { useLocation, useNavigate } from 'react-router-dom';
import { Box, Drawer, List, ListItemButton, ListItemIcon, ListItemText, Typography } from '@mui/material';
import { usePermissao } from '../hooks/usePermissao';
import { GRUPOS_NAVEGACAO, itemAtivo } from '../routes/navegacao';
import { layout } from '../styles/tokens';
import { MarcaAkteck } from './marca/MarcaAkteck';

const SIDEBAR_WIDTH = layout.sidebarWidth;

interface SidebarProps {
  /** Abaixo de 960px o menu vira gaveta temporária, aberta pelo botão do cabeçalho. */
  temporario: boolean;
  aberto: boolean;
  onFechar: () => void;
}

/**
 * Akteck Seg (ADR 0035) — menu agrupado por área (Visão geral / Clientes / Equipamentos /
 * Suporte / Administração), com a marca no topo. Grupos sem nenhum item visível para o
 * papel atual não aparecem.
 */
export function Sidebar({ temporario, aberto, onFechar }: SidebarProps) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const permissoes = usePermissao();

  const grupos = GRUPOS_NAVEGACAO.map((g) => ({ ...g, itens: g.itens.filter((i) => i.visivel(permissoes)) })).filter(
    (g) => g.itens.length > 0,
  );

  const conteudo = (
    <Box component="nav" aria-label="Menu principal" sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ height: layout.headerHeight, display: 'flex', alignItems: 'center', px: 2.5 }}>
        <MarcaAkteck tamanho={28} />
      </Box>
      <Box sx={{ flexGrow: 1, overflowY: 'auto', px: 1.5, pb: 2 }}>
        {grupos.map((grupo) => (
          <Box key={grupo.rotulo} sx={{ mt: 2 }}>
            <Typography variant="overline" component="p" sx={{ px: 1.5, mb: 0.5, color: 'text.secondary', display: 'block' }}>
              {grupo.rotulo}
            </Typography>
            <List disablePadding sx={{ display: 'flex', flexDirection: 'column', gap: 0.25 }}>
              {grupo.itens.map((item) => {
                const Icone = item.icone;
                const ativo = itemAtivo(pathname, item);
                return (
                  <ListItemButton
                    key={item.rota}
                    selected={ativo}
                    aria-current={ativo ? 'page' : undefined}
                    onClick={() => {
                      navigate(item.rota);
                      onFechar();
                    }}
                    sx={{ px: 1.5 }}
                  >
                    <ListItemIcon sx={{ minWidth: 34, color: ativo ? 'primary.main' : 'text.secondary' }}>
                      <Icone fontSize="small" />
                    </ListItemIcon>
                    <ListItemText
                      primary={item.rotulo}
                      slotProps={{ primary: { sx: { fontSize: 14, fontWeight: ativo ? 600 : 500 } } }}
                    />
                  </ListItemButton>
                );
              })}
            </List>
          </Box>
        ))}
      </Box>
    </Box>
  );

  return (
    <Drawer
      variant={temporario ? 'temporary' : 'permanent'}
      open={temporario ? aberto : true}
      onClose={onFechar}
      ModalProps={{ keepMounted: true }}
      sx={{
        width: temporario ? undefined : SIDEBAR_WIDTH,
        flexShrink: 0,
        '& .MuiDrawer-paper': { width: SIDEBAR_WIDTH, boxSizing: 'border-box', borderRight: 1, borderColor: 'divider' },
      }}
    >
      {conteudo}
    </Drawer>
  );
}
