import { useState } from 'react';
import { Alert, Box, Button, Chip, Paper, Typography } from '@mui/material';
import PersonAddAlt1OutlinedIcon from '@mui/icons-material/PersonAddAlt1Outlined';
import BadgeOutlinedIcon from '@mui/icons-material/BadgeOutlined';
import { TabelaPadrao, type ColunaTabela } from '../../compartilhado/componentes/TabelaPadrao';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useAuth } from '../../hooks/useAuth';
import { extrairMensagemErro } from '../../services/httpClient';
import { useToastStore } from '../../stores/toastStore';
import { NovoMembroDialog } from './NovoMembroDialog';
import { useDesativarMembroMutation, useEquipeQuery } from './queries/useEquipe';
import { ROTULO_PAPEL } from './rotulosEquipe';
import type { MembroEquipe } from './types';

/** Akteck Seg (ADR 0035) — tela nova sobre `api/usuarios-internos` (existia desde a Sprint 21, sem interface). */
export function EquipeInternaPage() {
  const { user } = useAuth();
  const mostrarToast = useToastStore((s) => s.mostrar);
  const { data, isLoading, isError, refetch } = useEquipeQuery();
  const desativar = useDesativarMembroMutation();
  const [criando, setCriando] = useState(false);
  const [paraDesativar, setParaDesativar] = useState<MembroEquipe | null>(null);

  const colunas: ColunaTabela<MembroEquipe>[] = [
    {
      cabecalho: 'Nome',
      render: (m) => (
        <Box>
          <Typography sx={{ fontWeight: 500 }}>{m.nome}</Typography>
          <Typography variant="body2" color="text.secondary">
            {m.email}
          </Typography>
        </Box>
      ),
    },
    { cabecalho: 'Papel', render: (m) => ROTULO_PAPEL[m.roleGlobal] ?? m.roleGlobal },
    {
      cabecalho: 'Situação',
      render: (m) =>
        m.ativo ? <Chip size="small" color="success" variant="outlined" label="Ativo" /> : <Chip size="small" variant="outlined" label="Desativado" />,
    },
    { cabecalho: 'Desde', render: (m) => new Date(m.createdAtUtc).toLocaleDateString('pt-BR') },
    {
      cabecalho: 'Ações',
      align: 'right',
      render: (m) =>
        m.ativo && m.email !== user?.email ? (
          <Button size="small" color="error" onClick={() => setParaDesativar(m)}>
            Desativar
          </Button>
        ) : null,
    },
  ];

  const confirmarDesativacao = () => {
    if (!paraDesativar) return;
    desativar.mutate(paraDesativar.id, {
      onSuccess: () => {
        mostrarToast(`${paraDesativar.nome} não tem mais acesso ao painel.`, 'success');
        setParaDesativar(null);
      },
      onError: (e) => mostrarToast(extrairMensagemErro(e, 'Não foi possível desativar.'), 'error'),
    });
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="h1">Equipe interna</Typography>
          <Typography color="text.secondary">Quem da Akteck Seg tem acesso a este painel.</Typography>
        </Box>
        <Button variant="contained" startIcon={<PersonAddAlt1OutlinedIcon />} onClick={() => setCriando(true)}>
          Novo membro
        </Button>
      </Box>

      {isError ? (
        <Alert severity="error" action={<Button color="inherit" onClick={() => void refetch()}>Tentar de novo</Button>}>
          Não foi possível carregar a equipe.
        </Alert>
      ) : (
        <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
          <TabelaPadrao
            colunas={colunas}
            itens={data ?? []}
            chave={(m) => m.id}
            carregando={isLoading}
            vazio={{ icone: BadgeOutlinedIcon, titulo: 'Nenhum membro ainda', descricao: 'Adicione quem vai atender clientes ou instalar equipamentos.' }}
          />
        </Paper>
      )}

      <NovoMembroDialog
        aberto={criando}
        onFechar={(criado) => {
          setCriando(false);
          if (criado) mostrarToast('Membro criado. Envie a senha inicial para a pessoa por um canal seguro.', 'success');
        }}
      />
      <ConfirmDialog
        aberto={Boolean(paraDesativar)}
        titulo={`Desativar ${paraDesativar?.nome ?? ''}?`}
        mensagem="A pessoa perde o acesso ao painel na hora. O histórico de ações dela continua guardado."
        destrutivo
        carregando={desativar.isPending}
        onConfirmar={confirmarDesativacao}
        onCancelar={() => setParaDesativar(null)}
      />
    </Box>
  );
}
