import { Drawer, Box, Typography, List, ListItem, ListItemText, Chip, Skeleton, IconButton } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { EmptyState } from '../../components/EmptyState';
import HistoryIcon from '@mui/icons-material/History';
import { useHistoricoEquipamentoQuery } from './queries/useHistoricoEquipamentoQuery';

interface HistoricoDrawerProps {
  equipamentoId: string | null;
  equipamentoNome?: string | null;
  onFechar: () => void;
}

/** Sprint 22B (ADR 0031) — ciclo de vida completo de alocação de um equipamento (histórico nunca é apagado, ver entidade). */
export function HistoricoDrawer({ equipamentoId, equipamentoNome, onFechar }: HistoricoDrawerProps) {
  const { data: historico, isLoading } = useHistoricoEquipamentoQuery(equipamentoId);

  return (
    <Drawer anchor="right" open={Boolean(equipamentoId)} onClose={onFechar}>
      <Box sx={{ width: 380, p: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <Typography variant="h3">Histórico — {equipamentoNome}</Typography>
          <IconButton onClick={onFechar} size="small">
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>

        {isLoading ? (
          <Skeleton variant="rounded" height={200} />
        ) : historico && historico.length > 0 ? (
          <List>
            {historico.map((vinculo) => (
              <ListItem key={vinculo.id} divider alignItems="flex-start">
                <ListItemText
                  primary={vinculo.propriedadeNome}
                  secondary={
                    <>
                      {new Date(vinculo.dataInicioUtc).toLocaleString('pt-BR')} —{' '}
                      {vinculo.dataFimUtc ? new Date(vinculo.dataFimUtc).toLocaleString('pt-BR') : 'em andamento'}
                      {vinculo.observacoes ? <><br />{vinculo.observacoes}</> : null}
                    </>
                  }
                />
                {vinculo.ativo ? <Chip size="small" label="Ativo" color="success" /> : null}
              </ListItem>
            ))}
          </List>
        ) : (
          <EmptyState icone={HistoryIcon} titulo="Sem histórico" descricao="Este equipamento nunca foi provisionado." />
        )}
      </Box>
    </Drawer>
  );
}
