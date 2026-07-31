import { Drawer, Box, Typography, IconButton, Stack, Chip, Tooltip, Button, Divider } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import SyncIcon from '@mui/icons-material/Sync';
import type { DiagnosticoEquipamento } from './types';

interface DetalheEquipamentoDrawerProps {
  equipamento: DiagnosticoEquipamento | null;
  onFechar: () => void;
}

/**
 * Sprint 22B (ADR 0031) — drawer de detalhe do Diagnóstico. Os botões de ação de hardware
 * (Reiniciar/Sincronizar) são MOCKS VISUAIS DESABILITADOS de propósito — a missão desta Sprint
 * proíbe explicitamente qualquer comunicação direta com equipamentos; ativar esses botões de
 * verdade é escopo da Sprint 22C.
 */
export function DetalheEquipamentoDrawer({ equipamento, onFechar }: DetalheEquipamentoDrawerProps) {
  return (
    <Drawer anchor="right" open={Boolean(equipamento)} onClose={onFechar}>
      <Box sx={{ width: 380, p: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <Typography variant="h3">{equipamento?.equipamentoNome}</Typography>
          <IconButton onClick={onFechar} size="small">
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>

        {equipamento ? (
          <Stack spacing={2}>
            <Box>
              <Typography variant="caption" color="text.secondary">
                Propriedade
              </Typography>
              <Typography variant="body1">{equipamento.propriedadeNome}</Typography>
            </Box>

            <Box sx={{ display: 'flex', gap: 1 }}>
              <Chip size="small" label={equipamento.status} color={equipamento.status === 'Online' ? 'success' : 'error'} />
              <Chip size="small" label={equipamento.estadoOperacional} variant="outlined" />
              {equipamento.temProblemaAtivo ? <Chip size="small" label="Problema ativo" color="error" /> : null}
            </Box>

            <Box>
              <Typography variant="caption" color="text.secondary">
                Último ping
              </Typography>
              <Typography variant="body1">
                {equipamento.ultimoPingUtc ? new Date(equipamento.ultimoPingUtc).toLocaleString('pt-BR') : 'Sem registro'}
              </Typography>
            </Box>

            <Box>
              <Typography variant="caption" color="text.secondary">
                Eventos recentes (7 dias)
              </Typography>
              <Typography variant="body1">{equipamento.quantidadeEventosRecentes}</Typography>
            </Box>

            <Box>
              <Typography variant="caption" color="text.secondary">
                Último evento
              </Typography>
              <Typography variant="body1">
                {equipamento.ultimoEventoDescricao ?? 'Nenhum'}
                {equipamento.ultimoEventoEmUtc ? ` — ${new Date(equipamento.ultimoEventoEmUtc).toLocaleString('pt-BR')}` : ''}
              </Typography>
            </Box>

            <Divider />

            <Box>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                Ações de hardware (disponíveis em breve)
              </Typography>
              <Stack direction="row" spacing={1}>
                <Tooltip title="Comunicação direta com hardware ainda não implementada (Sprint 22C)">
                  <span>
                    <Button size="small" variant="outlined" startIcon={<SyncIcon />} disabled>
                      Sincronizar
                    </Button>
                  </span>
                </Tooltip>
                <Tooltip title="Comunicação direta com hardware ainda não implementada (Sprint 22C)">
                  <span>
                    <Button size="small" variant="outlined" startIcon={<RestartAltIcon />} disabled>
                      Reiniciar
                    </Button>
                  </span>
                </Tooltip>
              </Stack>
            </Box>
          </Stack>
        ) : null}
      </Box>
    </Drawer>
  );
}
