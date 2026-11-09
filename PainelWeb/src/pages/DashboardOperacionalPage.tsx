import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Box, Button, Typography, useTheme } from '@mui/material';
import AddHomeWorkOutlinedIcon from '@mui/icons-material/AddHomeWorkOutlined';
import { dashboardService } from '../services/dashboardService';
import { auditoriaService } from '../services/auditoriaService';
import { usePermissao } from '../hooks/usePermissao';
import { IndicadoresOperacionais } from '../modulos/dashboard/IndicadoresOperacionais';
import { ClientesAtencao } from '../modulos/dashboard/ClientesAtencao';
import { DistribuicaoBarras } from '../modulos/dashboard/DistribuicaoBarras';
import { GraficoNovosClientes } from '../modulos/dashboard/GraficoNovosClientes';
import { AtividadeRecente } from '../modulos/dashboard/AtividadeRecente';
import { SaudeSistemaCard } from '../modulos/dashboard/SaudeSistemaCard';
import { saudeService } from '../services/saudeService';
import {
  ORDEM_STATUS_EQUIPAMENTO,
  ROTULO_STATUS_EQUIPAMENTO,
  ROTULO_TIPO_PROPRIEDADE,
  corStatusEquipamento,
} from '../modulos/dashboard/rotulosDashboard';

/**
 * Akteck Seg (ADR 0035) — Visão geral do Master/Suporte. Orquestrador fino: busca os dados
 * e entrega cada região a um componente de `modulos/dashboard`. A fila "Clientes que
 * precisam de atenção" é a peça central (só para quem pode ler o diagnóstico).
 */
export function DashboardOperacionalPage() {
  const theme = useTheme();
  const navigate = useNavigate();
  const { podeGerenciarHardware, isMaster } = usePermissao();

  const operacional = useQuery({
    queryKey: ['dashboard-operacional'],
    queryFn: dashboardService.obterOperacional,
    refetchInterval: 60_000,
  });

  const atividade = useQuery({
    queryKey: ['auditoria-recente'],
    queryFn: () => auditoriaService.listarRecentes(8),
    refetchInterval: 60_000,
  });

  // Sprint 27 (ADR 0043) — saúde do sistema (backend, centrais, rotinas, backup).
  const saude = useQuery({
    queryKey: ['saude-sistema'],
    queryFn: saudeService.obter,
    refetchInterval: 30_000,
    retry: false,
  });

  const dados = operacional.data;
  const atualizadoEm = operacional.dataUpdatedAt
    ? new Date(operacional.dataUpdatedAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
    : null;

  const status = Object.entries(dados?.equipamentosPorStatus ?? {})
    .sort(([a], [b]) => ORDEM_STATUS_EQUIPAMENTO.indexOf(a) - ORDEM_STATUS_EQUIPAMENTO.indexOf(b))
    .map(([chave, valor]) => ({ chave, valor, rotulo: ROTULO_STATUS_EQUIPAMENTO[chave] ?? chave, cor: corStatusEquipamento(chave, theme) }));

  const tipos = Object.entries(dados?.propriedadesPorTipo ?? {})
    .sort(([, a], [, b]) => b - a)
    .map(([chave, valor]) => ({ chave, valor, rotulo: ROTULO_TIPO_PROPRIEDADE[chave] ?? chave, cor: theme.palette.primary.main }));

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="h1">Visão geral</Typography>
          <Typography variant="body1" color="text.secondary">
            {atualizadoEm ? `Atualizado às ${atualizadoEm}. Os números se renovam a cada minuto.` : 'Carregando os números da operação…'}
          </Typography>
        </Box>
        {isMaster ? (
          <Button variant="contained" startIcon={<AddHomeWorkOutlinedIcon />} onClick={() => navigate('/nova-instalacao')}>
            Nova instalação
          </Button>
        ) : null}
      </Box>

      <SaudeSistemaCard dados={saude.data} carregando={saude.isLoading} erro={saude.isError} />

      <IndicadoresOperacionais dados={dados} carregando={operacional.isLoading} podeVerEquipamentos={podeGerenciarHardware} />

      {podeGerenciarHardware ? <ClientesAtencao /> : null}

      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: 'minmax(0, 1fr)', lg: 'repeat(3, minmax(0, 1fr))' } }}>
        <DistribuicaoBarras
          titulo="Equipamentos por status"
          descricao="Como os equipamentos estão respondendo agora."
          itens={status}
          carregando={operacional.isLoading}
          vazio="Nenhum equipamento instalado ainda."
        />
        <DistribuicaoBarras
          titulo="Propriedades por tipo"
          descricao="Onde os clientes usam o sistema."
          itens={tipos}
          carregando={operacional.isLoading}
          vazio="Nenhuma propriedade cadastrada ainda."
        />
        <GraficoNovosClientes dados={dados?.novosClientesPorMes ?? []} carregando={operacional.isLoading} />
      </Box>

      <AtividadeRecente itens={atividade.data ?? []} carregando={atividade.isLoading} erro={atividade.isError} />
    </Box>
  );
}
