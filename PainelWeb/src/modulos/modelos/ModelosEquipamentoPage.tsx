import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Alert, Box, Button, Chip, Paper, Tab, Tabs, Typography } from '@mui/material';
import AddOutlinedIcon from '@mui/icons-material/AddOutlined';
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined';
import { TabelaPadrao, type ColunaTabela } from '../../compartilhado/componentes/TabelaPadrao';
import type { FabricanteEquipamento } from '../../compartilhado/tipos/equipamento';
import { useToastStore } from '../../stores/toastStore';
import { CapacidadesDialog, NovoModeloDialog } from './ModeloDialogs';
import { useModelosQuery } from './queries/useModelos';
import { FABRICANTES, rotuloCapacidade, rotuloFabricante } from './rotulosModelos';
import type { ModeloEquipamento } from './types';

/**
 * Akteck Seg (ADR 0035) — tela nova sobre `api/modelos-equipamento` (Sprint 21, ADR 0027).
 * O filtro de fabricante vive na URL (`?fabricante=Jfl`), então dá para compartilhar o link.
 */
export function ModelosEquipamentoPage() {
  const mostrarToast = useToastStore((s) => s.mostrar);
  const [params, setParams] = useSearchParams();
  const fabricante = (params.get('fabricante') as FabricanteEquipamento | null) ?? undefined;
  const { data, isLoading, isError, refetch } = useModelosQuery(fabricante);
  const [criando, setCriando] = useState(false);
  const [editando, setEditando] = useState<ModeloEquipamento | null>(null);

  const colunas: ColunaTabela<ModeloEquipamento>[] = [
    { cabecalho: 'Modelo', render: (m) => <Typography sx={{ fontWeight: 500 }}>{m.nome}</Typography> },
    { cabecalho: 'Fabricante', render: (m) => rotuloFabricante(m.fabricante) },
    {
      cabecalho: 'Capacidades',
      render: (m) =>
        m.capacidades.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            Nenhuma definida
          </Typography>
        ) : (
          <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
            {m.capacidades.map((c) => (
              <Chip key={c} size="small" variant="outlined" label={rotuloCapacidade(c)} />
            ))}
          </Box>
        ),
    },
    {
      cabecalho: 'Ações',
      align: 'right',
      render: (m) => (
        <Button size="small" onClick={() => setEditando(m)}>
          Editar capacidades
        </Button>
      ),
    },
  ];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="h1">Catálogo de modelos</Typography>
          <Typography color="text.secondary">Os modelos de equipamento que a Akteck Seg instala e o que cada um sabe fazer.</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddOutlinedIcon />} onClick={() => setCriando(true)}>
          Novo modelo
        </Button>
      </Box>

      <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
        <Tabs
          value={fabricante ?? 'todos'}
          onChange={(_, valor: string) => setParams(valor === 'todos' ? {} : { fabricante: valor })}
          sx={{ px: 1.5, borderBottom: 1, borderColor: 'divider' }}
          aria-label="Filtrar por fabricante"
        >
          <Tab value="todos" label="Todos" />
          {FABRICANTES.map((f) => (
            <Tab key={f.valor} value={f.valor} label={f.rotulo} />
          ))}
        </Tabs>
        {isError ? (
          <Box sx={{ p: 2.5 }}>
            <Alert severity="error" action={<Button color="inherit" onClick={() => void refetch()}>Tentar de novo</Button>}>
              Não foi possível carregar o catálogo.
            </Alert>
          </Box>
        ) : (
          <TabelaPadrao
            colunas={colunas}
            itens={data ?? []}
            chave={(m) => m.id}
            carregando={isLoading}
            vazio={{
              icone: CategoryOutlinedIcon,
              titulo: fabricante ? `Nenhum modelo ${rotuloFabricante(fabricante)} ainda` : 'Nenhum modelo cadastrado',
              descricao: 'Cadastre os modelos que vocês instalam para definir o que cada um sabe fazer.',
            }}
          />
        )}
      </Paper>

      <NovoModeloDialog
        aberto={criando}
        onFechar={(criado) => {
          setCriando(false);
          if (criado) mostrarToast('Modelo criado. Agora defina as capacidades dele.', 'success');
        }}
      />
      <CapacidadesDialog
        key={editando?.id ?? 'fechado'}
        modelo={editando}
        onFechar={(salvo) => {
          setEditando(null);
          if (salvo) mostrarToast('Capacidades atualizadas.', 'success');
        }}
      />
    </Box>
  );
}
