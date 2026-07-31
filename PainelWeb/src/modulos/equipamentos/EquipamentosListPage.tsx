import { useState } from 'react';
import { Box, Button } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DevicesIcon from '@mui/icons-material/Devices';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import IconButton from '@mui/material/IconButton';
import { CabecalhoPagina } from '../../compartilhado/componentes/CabecalhoPagina';
import { BarraPesquisa } from '../../compartilhado/componentes/BarraPesquisa';
import { PaginacaoPadrao } from '../../compartilhado/componentes/PaginacaoPadrao';
import { TabelaPadrao, type ColunaTabela } from '../../compartilhado/componentes/TabelaPadrao';
import { BadgeStatus, SeletorStatus } from '../../compartilhado/componentes/BadgeStatus';
import { useDebounce } from '../../compartilhado/hooks/useDebounce';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useToastStore } from '../../stores/toastStore';
import { extrairMensagemErro } from '../../services/httpClient';
import { useEquipamentosQuery } from './queries/useEquipamentosQuery';
import { useExcluirEquipamentoMutation } from './mutations/useExcluirEquipamentoMutation';
import { EquipamentoFormDialog } from './EquipamentoFormDialog';
import { DetalheEquipamentoDrawer } from './DetalheEquipamentoDrawer';
import type { EquipamentoAdmin, EstadoOperacionalEquipamento, FabricanteEquipamento } from './types';

const TAMANHO_PAGINA = 20;

const ESTADO_OPCOES = [
  { valor: 'Ativo' as const, rotulo: 'Ativo', cor: 'success' as const },
  { valor: 'EmManutencao' as const, rotulo: 'Em Manutenção', cor: 'warning' as const },
  { valor: 'Inativo' as const, rotulo: 'Inativo', cor: 'default' as const },
  { valor: 'Defeituoso' as const, rotulo: 'Defeituoso', cor: 'error' as const },
];

const STATUS_OPCOES = [
  { valor: 'Online' as const, rotulo: 'Online', cor: 'success' as const },
  { valor: 'Offline' as const, rotulo: 'Offline', cor: 'error' as const },
  { valor: 'Desconhecido' as const, rotulo: 'Desconhecido', cor: 'default' as const },
];

const FABRICANTE_OPCOES = [
  { valor: 'Jfl' as const, rotulo: 'JFL', cor: 'default' as const },
  { valor: 'ControlId' as const, rotulo: 'Control iD', cor: 'default' as const },
  { valor: 'Intelbras' as const, rotulo: 'Intelbras', cor: 'default' as const },
];

/** Sprint 22B (ADR 0031) — CRUD global de Equipamentos (cross-propriedade), Master/Técnico-only. */
export function EquipamentosListPage() {
  const [pagina, setPagina] = useState(1);
  const [busca, setBusca] = useState('');
  const buscaDebounced = useDebounce(busca, 300);
  const [fabricante, setFabricante] = useState<FabricanteEquipamento | ''>('');
  const [estadoOperacional, setEstadoOperacional] = useState<EstadoOperacionalEquipamento | ''>('');
  const [equipamentoEditando, setEquipamentoEditando] = useState<EquipamentoAdmin | null>(null);
  const [formAberto, setFormAberto] = useState(false);
  const [equipamentoExcluindo, setEquipamentoExcluindo] = useState<EquipamentoAdmin | null>(null);
  const [equipamentoDetalhe, setEquipamentoDetalhe] = useState<EquipamentoAdmin | null>(null);

  const mostrarToast = useToastStore((s) => s.mostrar);
  const filtro = { busca: buscaDebounced || undefined, fabricante, estadoOperacional };
  const { data, isLoading } = useEquipamentosQuery(pagina, TAMANHO_PAGINA, filtro);
  const excluirMutation = useExcluirEquipamentoMutation();

  const colunas: ColunaTabela<EquipamentoAdmin>[] = [
    { cabecalho: 'Nome', render: (e) => e.nome },
    { cabecalho: 'Propriedade', render: (e) => e.propriedadeNome ?? '—' },
    { cabecalho: 'Fabricante', render: (e) => e.fabricante },
    { cabecalho: 'Número de Série', render: (e) => e.numeroSerie ?? '—' },
    { cabecalho: 'Conectividade', render: (e) => <BadgeStatus valor={e.status} opcoes={STATUS_OPCOES} /> },
    { cabecalho: 'Estado', render: (e) => <BadgeStatus valor={e.estadoOperacional} opcoes={ESTADO_OPCOES} /> },
    {
      cabecalho: '',
      align: 'right',
      render: (e) => (
        <>
          <IconButton
            size="small"
            onClick={(evento) => {
              evento.stopPropagation();
              setEquipamentoEditando(e);
              setFormAberto(true);
            }}
          >
            <EditIcon fontSize="small" />
          </IconButton>
          <IconButton
            size="small"
            onClick={(evento) => {
              evento.stopPropagation();
              setEquipamentoExcluindo(e);
            }}
          >
            <DeleteIcon fontSize="small" />
          </IconButton>
        </>
      ),
    },
  ];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <CabecalhoPagina
        titulo="Equipamentos"
        breadcrumbs={[{ rotulo: 'Dashboard', rota: '/dashboard' }, { rotulo: 'Equipamentos' }]}
        acao={
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => {
              setEquipamentoEditando(null);
              setFormAberto(true);
            }}
          >
            Novo Equipamento
          </Button>
        }
      />

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
        <BarraPesquisa value={busca} onChange={setBusca} placeholder="Buscar por nome ou número de série" />
        <SeletorStatus
          label="Fabricante"
          value={fabricante}
          onChange={(v) => setFabricante(v)}
          opcoes={FABRICANTE_OPCOES}
          todosRotulo="Todos"
        />
        <SeletorStatus
          label="Estado Operacional"
          value={estadoOperacional}
          onChange={(v) => setEstadoOperacional(v)}
          opcoes={ESTADO_OPCOES}
          todosRotulo="Todos"
        />
      </Box>

      <TabelaPadrao
        colunas={colunas}
        itens={data?.itens ?? []}
        chave={(e) => e.id}
        carregando={isLoading}
        onRowClick={(e) => setEquipamentoDetalhe(e)}
        vazio={{
          icone: DevicesIcon,
          titulo: busca ? 'Nenhum equipamento encontrado' : 'Nenhum equipamento cadastrado',
          descricao: busca
            ? 'Tente buscar por outro nome ou número de série.'
            : 'Cadastre o primeiro equipamento para começar.',
        }}
      />

      <PaginacaoPadrao paginaAtual={pagina} totalPaginas={data?.totalPaginas ?? 0} onChange={setPagina} />

      <EquipamentoFormDialog
        aberto={formAberto}
        equipamento={equipamentoEditando}
        onFechar={() => setFormAberto(false)}
      />

      <DetalheEquipamentoDrawer
        equipamento={equipamentoDetalhe}
        onFechar={() => setEquipamentoDetalhe(null)}
        onEditar={(e) => {
          setEquipamentoDetalhe(null);
          setEquipamentoEditando(e);
          setFormAberto(true);
        }}
      />

      <ConfirmDialog
        aberto={Boolean(equipamentoExcluindo)}
        titulo="Excluir equipamento"
        mensagem={`Tem certeza que deseja excluir "${equipamentoExcluindo?.nome}"? Esta ação pode ser desfeita apenas por um Master via banco de dados.`}
        destrutivo
        carregando={excluirMutation.isPending}
        onCancelar={() => setEquipamentoExcluindo(null)}
        onConfirmar={() => {
          if (!equipamentoExcluindo) return;
          excluirMutation.mutate(equipamentoExcluindo.id, {
            onSuccess: () => {
              mostrarToast('Equipamento excluído.', 'success');
              setEquipamentoExcluindo(null);
            },
            onError: (erro) => {
              mostrarToast(extrairMensagemErro(erro, 'Não foi possível excluir o equipamento.'), 'error');
              setEquipamentoExcluindo(null);
            },
          });
        }}
      />
    </Box>
  );
}
