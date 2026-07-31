import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Box, Grid, Paper, Typography, Button, Stack } from '@mui/material';
import WifiOffIcon from '@mui/icons-material/WifiOff';
import BuildIcon from '@mui/icons-material/Build';
import SupportAgentIcon from '@mui/icons-material/SupportAgent';
import { dashboardService } from '../services/dashboardService';
import { StatCard } from '../components/StatCard';
import { EmptyState } from '../components/EmptyState';
import { usePermissao } from '../hooks/usePermissao';

/**
 * Sprint 22A (Fase 4) — landing do Técnico. "Meus Clientes"/"Minhas Instalações" da missão
 * original pressupõem um vínculo Técnico↔Provisionamento que não existe no domínio
 * (`Provisionamento` não tem campo de responsável, ver docs/painel/mapeamento-api.md) — em vez de
 * fabricar dado, mostra um estado honesto explicando a limitação. "Equipamentos Offline" é real
 * (mesmo agregado do Dashboard Operacional, aberto para qualquer interno, ver ADR 0029).
 */
export function DashboardTecnicoPage() {
  const navigate = useNavigate();
  const { podeImpersonar } = usePermissao();

  const { data: dashboard, isLoading } = useQuery({
    queryKey: ['dashboard-operacional'],
    queryFn: dashboardService.obterOperacional,
    refetchInterval: 60_000,
  });

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Typography variant="h1">Dashboard Técnico</Typography>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 6, md: 4 }}>
          <StatCard
            titulo="Equipamentos Offline"
            valor={dashboard?.totalEquipamentosOffline ?? 0}
            icone={WifiOffIcon}
            cor="error"
            carregando={isLoading}
          />
        </Grid>
      </Grid>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="h3" sx={{ mb: 1 }}>
          Ações Rápidas
        </Typography>
        <Stack direction="row" spacing={2}>
          {podeImpersonar ? (
            <Button
              variant="outlined"
              startIcon={<SupportAgentIcon />}
              onClick={() => navigate('/suporte/selecionar-cliente')}
            >
              Ir para Suporte
            </Button>
          ) : null}
        </Stack>
      </Paper>

      <Paper variant="outlined">
        <EmptyState
          icone={BuildIcon}
          titulo="Minhas Instalações ainda não é rastreável"
          descricao="Provisionamentos não têm um técnico responsável vinculado hoje — essa atribuição fica para uma Sprint futura (ver ADR 0028/0029). Por ora, provisionamentos são acessados por propriedade específica."
        />
      </Paper>
    </Box>
  );
}
