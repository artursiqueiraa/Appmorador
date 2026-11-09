import { Box, Button, Paper, Skeleton, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import type { AuditoriaMasterItem } from '../../types/api';
import { descreverAtividade, ehAlertaDeSeguranca, formatarDataHora } from './rotulosDashboard';

/** Akteck Seg (ADR 0035) — "Carlos editou um equipamento · 14:02", com acesso negado sinalizado em vermelho. */
export function AtividadeRecente({
  itens,
  carregando,
  erro,
}: {
  itens: AuditoriaMasterItem[];
  carregando: boolean;
  erro: boolean;
}) {
  const navigate = useNavigate();

  return (
    <Paper variant="outlined" sx={{ p: 2.5, height: '100%' }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 2, mb: 1.5 }}>
        <Box>
          <Typography variant="h3" component="h2">
            Atividade recente
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Últimas ações da equipe no painel.
          </Typography>
        </Box>
        <Button variant="text" onClick={() => navigate('/suporte/logs')}>
          Ver tudo
        </Button>
      </Box>
      {carregando ? (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          {[0, 1, 2, 3].map((i) => (
            <Skeleton key={i} variant="text" height={36} />
          ))}
        </Box>
      ) : erro ? (
        <Typography color="error.main">Não foi possível carregar a atividade recente.</Typography>
      ) : itens.length === 0 ? (
        <Typography color="text.secondary">Nenhuma ação registrada ainda. Logins, edições e acessos aparecem aqui.</Typography>
      ) : (
        <Box component="ol" sx={{ listStyle: 'none', m: 0, p: 0 }}>
          {itens.map((item) => {
            const alerta = ehAlertaDeSeguranca(item);
            return (
              <Box
                component="li"
                key={item.id}
                sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, py: 1.25, borderTop: 1, borderColor: 'divider' }}
              >
                <Typography variant="body1" sx={{ color: alerta ? 'error.main' : 'text.primary', minWidth: 0 }}>
                  {descreverAtividade(item)}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'nowrap' }}>
                  {formatarDataHora(item.dataHoraUtc)}
                </Typography>
              </Box>
            );
          })}
        </Box>
      )}
    </Paper>
  );
}
