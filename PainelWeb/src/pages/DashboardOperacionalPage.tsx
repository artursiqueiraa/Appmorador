import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Grid, Paper, Typography, Box, List, ListItem, ListItemText, Chip } from '@mui/material';
import PeopleIcon from '@mui/icons-material/People';
import HomeWorkIcon from '@mui/icons-material/HomeWork';
import DevicesIcon from '@mui/icons-material/Devices';
import WifiOffIcon from '@mui/icons-material/WifiOff';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend } from 'recharts';
import { dashboardService } from '../services/dashboardService';
import { auditoriaService } from '../services/auditoriaService';
import { StatCard } from '../components/StatCard';
import { EmptyState } from '../components/EmptyState';
import { colors } from '../styles/tokens';

const CORES_GRAFICO = [colors.primary, colors.info, colors.warning, colors.error, colors.textMuted];

/**
 * Sprint 22A (Fase 3) — landing do Master/Suporte. "Total de Clientes" é o único card clicável
 * com destino real nesta Sprint (Propriedades/Equipamentos completos são Sprint 22B, ver
 * ADR 0029) — os outros 3 são informativos, sem link quebrado fingindo uma tela que não existe
 * ainda.
 */
export function DashboardOperacionalPage() {
  const navigate = useNavigate();

  const { data: dashboard, isLoading } = useQuery({
    queryKey: ['dashboard-operacional'],
    queryFn: dashboardService.obterOperacional,
    refetchInterval: 60_000,
  });

  const { data: atividadeRecente, isLoading: carregandoAtividade } = useQuery({
    queryKey: ['auditoria-recente'],
    queryFn: () => auditoriaService.listarRecentes(8),
    refetchInterval: 60_000,
  });

  const dadosNovosClientes = (dashboard?.novosClientesPorMes ?? []).map((i) => ({
    mes: i.mes,
    quantidade: i.quantidade,
  }));
  const dadosPropriedadesPorTipo = Object.entries(dashboard?.propriedadesPorTipo ?? {}).map(([nome, valor]) => ({
    nome,
    valor,
  }));
  const dadosEquipamentosPorStatus = Object.entries(dashboard?.equipamentosPorStatus ?? {}).map(([nome, valor]) => ({
    nome,
    valor,
  }));

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Typography variant="h1">Dashboard Operacional</Typography>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard
            titulo="Total de Clientes"
            valor={dashboard?.totalClientes ?? 0}
            icone={PeopleIcon}
            carregando={isLoading}
            onClick={() => navigate('/clientes')}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard
            titulo="Total de Propriedades"
            valor={dashboard?.totalPropriedades ?? 0}
            icone={HomeWorkIcon}
            carregando={isLoading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard
            titulo="Total de Equipamentos"
            valor={dashboard?.totalEquipamentos ?? 0}
            icone={DevicesIcon}
            carregando={isLoading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard
            titulo="Equipamentos Offline"
            valor={dashboard?.totalEquipamentosOffline ?? 0}
            icone={WifiOffIcon}
            cor="error"
            carregando={isLoading}
          />
        </Grid>
      </Grid>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 5 }}>
          <Paper variant="outlined" sx={{ p: 2, height: 320 }}>
            <Typography variant="h3" sx={{ mb: 2 }}>
              Novos Clientes por Mês
            </Typography>
            <ResponsiveContainer width="100%" height="85%">
              <BarChart data={dadosNovosClientes}>
                <XAxis dataKey="mes" fontSize={12} />
                <YAxis allowDecimals={false} fontSize={12} />
                <Tooltip />
                <Bar dataKey="quantidade" fill={colors.primary} radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 3.5 }}>
          <Paper variant="outlined" sx={{ p: 2, height: 320 }}>
            <Typography variant="h3" sx={{ mb: 2 }}>
              Propriedades por Tipo
            </Typography>
            <ResponsiveContainer width="100%" height="85%">
              <PieChart>
                <Pie data={dadosPropriedadesPorTipo} dataKey="valor" nameKey="nome" innerRadius={40} outerRadius={70}>
                  {dadosPropriedadesPorTipo.map((entrada, indice) => (
                    <Cell key={entrada.nome} fill={CORES_GRAFICO[indice % CORES_GRAFICO.length]} />
                  ))}
                </Pie>
                <Legend />
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 3.5 }}>
          <Paper variant="outlined" sx={{ p: 2, height: 320 }}>
            <Typography variant="h3" sx={{ mb: 2 }}>
              Equipamentos por Status
            </Typography>
            <ResponsiveContainer width="100%" height="85%">
              <PieChart>
                <Pie data={dadosEquipamentosPorStatus} dataKey="valor" nameKey="nome" innerRadius={40} outerRadius={70}>
                  {dadosEquipamentosPorStatus.map((entrada, indice) => (
                    <Cell key={entrada.nome} fill={CORES_GRAFICO[indice % CORES_GRAFICO.length]} />
                  ))}
                </Pie>
                <Legend />
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>
      </Grid>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="h3" sx={{ mb: 1 }}>
          Atividade Recente
        </Typography>
        {!carregandoAtividade && atividadeRecente?.length === 0 ? (
          <EmptyState
            icone={PeopleIcon}
            titulo="Nenhuma atividade ainda"
            descricao="As últimas ações registradas na plataforma aparecem aqui."
          />
        ) : (
          <List dense>
            {(atividadeRecente ?? []).map((item) => (
              <ListItem key={item.id} divider>
                <ListItemText
                  primary={`${item.usuarioNome} — ${item.acao}`}
                  secondary={new Date(item.dataHoraUtc).toLocaleString('pt-BR')}
                />
                {item.entidade ? <Chip size="small" label={item.entidade} variant="outlined" /> : null}
              </ListItem>
            ))}
          </List>
        )}
      </Paper>
    </Box>
  );
}
