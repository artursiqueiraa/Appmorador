import { useState } from 'react';
import { Box, Typography } from '@mui/material';
import MonitorHeartIcon from '@mui/icons-material/MonitorHeart';
import { CabecalhoPagina } from '../../compartilhado/componentes/CabecalhoPagina';
import { PaginacaoPadrao } from '../../compartilhado/componentes/PaginacaoPadrao';
import { TabelaPadrao, type ColunaTabela } from '../../compartilhado/componentes/TabelaPadrao';
import { BadgeStatus, SeletorStatus } from '../../compartilhado/componentes/BadgeStatus';
import { useDiagnosticoEquipamentosQuery } from './queries/useDiagnosticoEquipamentosQuery';
import { DetalheEquipamentoDrawer } from './DetalheEquipamentoDrawer';
import type { DiagnosticoEquipamento } from './types';

const TAMANHO_PAGINA = 20;

const STATUS_OPCOES = [
  { valor: 'Online' as const, rotulo: 'Online', cor: 'success' as const },
  { valor: 'Offline' as const, rotulo: 'Offline', cor: 'error' as const },
  { valor: 'Desconhecido' as const, rotulo: 'Desconhecido', cor: 'default' as const },
];

/** Cada opção é o intervalo de polling em ms — nulo = "Desligado". Padrão do app: 30s. */
const OPCOES_POLLING = [
  { valor: '' as const, rotulo: 'Desligado', cor: 'default' as const },
  { valor: '10000' as const, rotulo: 'A cada 10s', cor: 'default' as const },
  { valor: '30000' as const, rotulo: 'A cada 30s', cor: 'default' as const },
  { valor: '60000' as const, rotulo: 'A cada 60s', cor: 'default' as const },
];

/** Sprint 22B (ADR 0031) — monitoramento operacional agregado (Equipamento + StatusCentralJfl + EventoEquipamento), estritamente leitura. */
export function DiagnosticoEquipamentosPage() {
  const [pagina, setPagina] = useState(1);
  const [pollingMs, setPollingMs] = useState<'' | '10000' | '30000' | '60000'>('30000');
  const [equipamentoSelecionado, setEquipamentoSelecionado] = useState<DiagnosticoEquipamento | null>(null);

  const { data, isLoading, isFetching } = useDiagnosticoEquipamentosQuery(
    pagina,
    TAMANHO_PAGINA,
    pollingMs ? Number(pollingMs) : null,
  );

  const colunas: ColunaTabela<DiagnosticoEquipamento>[] = [
    { cabecalho: 'Equipamento', render: (e) => e.equipamentoNome },
    { cabecalho: 'Propriedade', render: (e) => e.propriedadeNome },
    { cabecalho: 'Conectividade', render: (e) => <BadgeStatus valor={e.status} opcoes={STATUS_OPCOES} /> },
    {
      cabecalho: 'Último ping',
      render: (e) => (e.ultimoPingUtc ? new Date(e.ultimoPingUtc).toLocaleString('pt-BR') : '—'),
    },
    { cabecalho: 'Eventos (7d)', render: (e) => e.quantidadeEventosRecentes },
  ];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <CabecalhoPagina
        titulo="Diagnóstico de Equipamentos"
        breadcrumbs={[{ rotulo: 'Dashboard', rota: '/dashboard' }, { rotulo: 'Diagnóstico' }]}
      />

      <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
        <SeletorStatus
          label="Atualização automática"
          value={pollingMs}
          onChange={(v) => setPollingMs(v as typeof pollingMs)}
          opcoes={OPCOES_POLLING}
        />
        {isFetching && !isLoading ? (
          <Typography variant="caption" color="text.secondary">
            Atualizando…
          </Typography>
        ) : null}
      </Box>

      <TabelaPadrao
        colunas={colunas}
        itens={data?.itens ?? []}
        chave={(e) => e.equipamentoId}
        carregando={isLoading}
        onRowClick={setEquipamentoSelecionado}
        vazio={{
          icone: MonitorHeartIcon,
          titulo: 'Nenhum equipamento cadastrado',
          descricao: 'Cadastre equipamentos no módulo Equipamentos para monitorá-los aqui.',
        }}
      />

      <PaginacaoPadrao paginaAtual={pagina} totalPaginas={data?.totalPaginas ?? 0} onChange={setPagina} />

      <DetalheEquipamentoDrawer equipamento={equipamentoSelecionado} onFechar={() => setEquipamentoSelecionado(null)} />
    </Box>
  );
}
