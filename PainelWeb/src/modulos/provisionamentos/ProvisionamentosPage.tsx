import { useState } from 'react';
import { Box, Button, Grid, IconButton } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import SwapHorizIcon from '@mui/icons-material/SwapHoriz';
import LinkOffIcon from '@mui/icons-material/LinkOff';
import HistoryIcon from '@mui/icons-material/History';
import DevicesIcon from '@mui/icons-material/Devices';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import Inventory2Icon from '@mui/icons-material/Inventory2';
import { CabecalhoPagina } from '../../compartilhado/componentes/CabecalhoPagina';
import { PaginacaoPadrao } from '../../compartilhado/componentes/PaginacaoPadrao';
import { TabelaPadrao, type ColunaTabela } from '../../compartilhado/componentes/TabelaPadrao';
import { StatCard } from '../../components/StatCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useToastStore } from '../../stores/toastStore';
import { extrairMensagemErro } from '../../services/httpClient';
import { useDashboardAlocacaoQuery } from './queries/useDashboardAlocacaoQuery';
import { useProvisionamentosAtivosQuery } from './queries/useProvisionamentosAtivosQuery';
import { useDesvincularMutation } from './mutations/useDesvincularMutation';
import { ProvisionarDialog } from './ProvisionarDialog';
import { TrocarDialog } from './TrocarDialog';
import { HistoricoDrawer } from './HistoricoDrawer';
import type { Vinculo } from './types';

const TAMANHO_PAGINA = 20;

/** Sprint 22B (ADR 0031) — visão de alocação Equipamento↔Propriedade, Master/Técnico-only. */
export function ProvisionamentosPage() {
  const [pagina, setPagina] = useState(1);
  const [wizardAberto, setWizardAberto] = useState(false);
  const [vinculoTrocando, setVinculoTrocando] = useState<Vinculo | null>(null);
  const [vinculoDesvinculando, setVinculoDesvinculando] = useState<Vinculo | null>(null);
  const [historicoEquipamento, setHistoricoEquipamento] = useState<{ id: string; nome?: string | null } | null>(null);

  const mostrarToast = useToastStore((s) => s.mostrar);
  const { data: dashboard, isLoading: carregandoDashboard } = useDashboardAlocacaoQuery();
  const { data, isLoading } = useProvisionamentosAtivosQuery(pagina, TAMANHO_PAGINA);
  const desvincularMutation = useDesvincularMutation();

  const colunas: ColunaTabela<Vinculo>[] = [
    { cabecalho: 'Equipamento', render: (v) => v.equipamentoNome ?? '—' },
    { cabecalho: 'Propriedade', render: (v) => v.propriedadeNome ?? '—' },
    { cabecalho: 'Desde', render: (v) => new Date(v.dataInicioUtc).toLocaleDateString('pt-BR') },
    { cabecalho: 'Observações', render: (v) => v.observacoes ?? '—' },
    {
      cabecalho: '',
      align: 'right',
      render: (v) => (
        <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'flex-end' }}>
          <IconButton
            size="small"
            title="Histórico"
            onClick={(e) => {
              e.stopPropagation();
              setHistoricoEquipamento({ id: v.equipamentoId, nome: v.equipamentoNome });
            }}
          >
            <HistoryIcon fontSize="small" />
          </IconButton>
          <IconButton
            size="small"
            title="Trocar equipamento"
            onClick={(e) => {
              e.stopPropagation();
              setVinculoTrocando(v);
            }}
          >
            <SwapHorizIcon fontSize="small" />
          </IconButton>
          <IconButton
            size="small"
            title="Desvincular"
            onClick={(e) => {
              e.stopPropagation();
              setVinculoDesvinculando(v);
            }}
          >
            <LinkOffIcon fontSize="small" />
          </IconButton>
        </Box>
      ),
    },
  ];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <CabecalhoPagina
        titulo="Provisionamentos"
        breadcrumbs={[{ rotulo: 'Dashboard', rota: '/dashboard' }, { rotulo: 'Provisionamentos' }]}
        acao={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setWizardAberto(true)}>
            Provisionar Equipamento
          </Button>
        }
      />

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatCard titulo="Total de Equipamentos" valor={dashboard?.totalEquipamentos ?? 0} icone={DevicesIcon} carregando={carregandoDashboard} />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatCard titulo="Provisionados" valor={dashboard?.totalProvisionados ?? 0} icone={CheckCircleIcon} cor="success" carregando={carregandoDashboard} />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatCard titulo="Disponíveis" valor={dashboard?.totalDisponiveis ?? 0} icone={Inventory2Icon} carregando={carregandoDashboard} />
        </Grid>
      </Grid>

      <TabelaPadrao
        colunas={colunas}
        itens={data?.itens ?? []}
        chave={(v) => v.id}
        carregando={isLoading}
        vazio={{
          icone: Inventory2Icon,
          titulo: 'Nenhum equipamento provisionado',
          descricao: 'Provisione um equipamento para começar a alocação.',
        }}
      />

      <PaginacaoPadrao paginaAtual={pagina} totalPaginas={data?.totalPaginas ?? 0} onChange={setPagina} />

      <ProvisionarDialog aberto={wizardAberto} onFechar={() => setWizardAberto(false)} />

      <TrocarDialog vinculo={vinculoTrocando} onFechar={() => setVinculoTrocando(null)} />

      <HistoricoDrawer
        equipamentoId={historicoEquipamento?.id ?? null}
        equipamentoNome={historicoEquipamento?.nome}
        onFechar={() => setHistoricoEquipamento(null)}
      />

      <ConfirmDialog
        aberto={Boolean(vinculoDesvinculando)}
        titulo="Desvincular equipamento"
        mensagem={`Tem certeza que deseja desvincular "${vinculoDesvinculando?.equipamentoNome}" de "${vinculoDesvinculando?.propriedadeNome}"?`}
        destrutivo
        carregando={desvincularMutation.isPending}
        onCancelar={() => setVinculoDesvinculando(null)}
        onConfirmar={() => {
          if (!vinculoDesvinculando) return;
          desvincularMutation.mutate(vinculoDesvinculando.equipamentoId, {
            onSuccess: () => {
              mostrarToast('Equipamento desvinculado.', 'success');
              setVinculoDesvinculando(null);
            },
            onError: (erro) => {
              mostrarToast(extrairMensagemErro(erro, 'Não foi possível desvincular o equipamento.'), 'error');
              setVinculoDesvinculando(null);
            },
          });
        }}
      />
    </Box>
  );
}
