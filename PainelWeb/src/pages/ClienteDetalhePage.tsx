import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Box, Typography, Paper, Chip, Button, Skeleton } from '@mui/material';
import LoginIcon from '@mui/icons-material/Login';
import HomeWorkIcon from '@mui/icons-material/HomeWork';
import { proprietariosService } from '../services/proprietariosService';
import { authService } from '../services/authService';
import { useAuthStore } from '../stores/authStore';
import { useToastStore } from '../stores/toastStore';
import { extrairMensagemErro } from '../services/httpClient';
import { Breadcrumbs } from '../components/Breadcrumbs';
import { EmptyState } from '../components/EmptyState';
import { usePermissao } from '../hooks/usePermissao';
import { RecursosPropriedade } from '../modulos/recursos/RecursosPropriedade';
import { SimuladorEventos } from '../modulos/simulador/SimuladorEventos';
import { ROTULO_TIPO_PROPRIEDADE } from '../compartilhado/tipos/propriedade';

/** Sprint 22A (Fase 5/6) — detalhe do cliente + entrada rápida para impersonation por propriedade específica. */
export function ClienteDetalhePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const mostrarToast = useToastStore((s) => s.mostrar);
  const startImpersonation = useAuthStore((s) => s.startImpersonation);
  const accessToken = useAuthStore((s) => s.accessToken);
  const { podeImpersonar, podeGerenciarHardware, isMaster } = usePermissao();

  const { data: cliente, isLoading } = useQuery({
    queryKey: ['proprietario', id],
    queryFn: () => proprietariosService.obterDetalhe(id!),
    enabled: Boolean(id),
  });

  const impersonarMutation = useMutation({
    mutationFn: (propriedadeId: string) => authService.impersonar({ propriedadeId }),
    onSuccess: (resposta) => {
      startImpersonation(resposta.accessToken, {
        propriedadeId: resposta.propriedadeId,
        propriedadeNome: resposta.propriedadeNome,
        clienteNome: resposta.clienteNome,
        tokenOriginal: accessToken!,
        expiresAtUtc: new Date(Date.now() + resposta.expiresInSeconds * 1000).toISOString(),
      });
      mostrarToast(`Acessando como ${resposta.clienteNome} — ${resposta.propriedadeNome}`, 'success');
      navigate('/suporte/diagnostico');
    },
    onError: (erro) =>
      mostrarToast(extrairMensagemErro(erro, 'Não foi possível iniciar a sessão de suporte.'), 'error'),
  });

  if (isLoading) return <Skeleton variant="rounded" height={300} />;
  if (!cliente)
    return (
      <EmptyState
        icone={HomeWorkIcon}
        titulo="Cliente não encontrado"
        descricao="Verifique o link e tente novamente."
      />
    );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Breadcrumbs itens={[{ rotulo: 'Clientes', rota: '/clientes' }, { rotulo: cliente.nome }]} />
      <Box sx={{ mt: -2 }}>
        <Typography variant="h1">{cliente.nome}</Typography>
        <Typography color="text.secondary">
          {cliente.email}, cliente desde {new Date(cliente.createdAtUtc).toLocaleDateString('pt-BR')}
        </Typography>
      </Box>
      <Box>
        <Chip
          size="small"
          variant="outlined"
          label={cliente.ativo ? 'Conta ativa' : 'Conta inativa'}
          color={cliente.ativo ? 'success' : 'default'}
        />
      </Box>

      <Typography variant="h2" component="h2">
        Propriedades ({cliente.propriedades.length})
      </Typography>
      {cliente.propriedades.length === 0 ? (
        <Paper variant="outlined">
          <EmptyState
            icone={HomeWorkIcon}
            titulo="Nenhuma propriedade"
            descricao="Este cliente ainda não cadastrou nenhuma propriedade."
          />
        </Paper>
      ) : (
        cliente.propriedades.map((propriedade) => (
          <Paper key={propriedade.id} variant="outlined" sx={{ p: 2.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
              <Box>
                <Typography variant="h3">{propriedade.nome}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {ROTULO_TIPO_PROPRIEDADE[propriedade.tipo] ?? propriedade.tipo}
                </Typography>
              </Box>
              {podeImpersonar ? (
                <Button
                  variant="outlined"
                  startIcon={<LoginIcon />}
                  loading={impersonarMutation.isPending && impersonarMutation.variables === propriedade.id}
                  onClick={() => impersonarMutation.mutate(propriedade.id)}
                >
                  Acessar como cliente
                </Button>
              ) : null}
            </Box>
            {podeGerenciarHardware ? (
              <Box sx={{ mt: 2 }}>
                <Typography variant="h5" component="h4" sx={{ mb: 1 }}>
                  Recursos contratados
                </Typography>
                <RecursosPropriedade propriedadeId={propriedade.id} />
              </Box>
            ) : null}
            {isMaster ? (
              <Box sx={{ mt: 2 }}>
                <Typography variant="h5" component="h4" sx={{ mb: 1 }}>
                  Simular evento (teste de alertas)
                </Typography>
                <SimuladorEventos propriedadeId={propriedade.id} />
              </Box>
            ) : null}
          </Paper>
        ))
      )}
    </Box>
  );
}
