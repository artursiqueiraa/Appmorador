import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Box, Typography, Paper, Chip, List, ListItem, ListItemText, Button, Skeleton, Stack } from '@mui/material';
import LoginIcon from '@mui/icons-material/Login';
import HomeWorkIcon from '@mui/icons-material/HomeWork';
import { proprietariosService } from '../services/proprietariosService';
import { authService } from '../services/authService';
import { useAuthStore } from '../stores/authStore';
import { useToastStore } from '../stores/toastStore';
import { extrairMensagemErro } from '../services/httpClient';
import { Breadcrumbs } from '../components/Breadcrumbs';
import { EmptyState } from '../components/EmptyState';

/** Sprint 22A (Fase 5/6) — detalhe do cliente + entrada rápida para impersonation por propriedade específica. */
export function ClienteDetalhePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const mostrarToast = useToastStore((s) => s.mostrar);
  const startImpersonation = useAuthStore((s) => s.startImpersonation);
  const accessToken = useAuthStore((s) => s.accessToken);

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
      mostrarToast(`Entrando como ${resposta.clienteNome} — ${resposta.propriedadeNome}`, 'success');
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
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Breadcrumbs
        itens={[
          { rotulo: 'Dashboard', rota: '/dashboard' },
          { rotulo: 'Clientes', rota: '/clientes' },
          { rotulo: cliente.nome },
        ]}
      />
      <Typography variant="h1">{cliente.nome}</Typography>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="h3" sx={{ mb: 1 }}>
          Dados Cadastrais
        </Typography>
        <Stack direction="row" spacing={4}>
          <Box>
            <Typography variant="caption" color="text.secondary">
              E-mail
            </Typography>
            <Typography variant="body1">{cliente.email}</Typography>
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary">
              Status
            </Typography>
            <Box>
              <Chip
                size="small"
                label={cliente.ativo ? 'Ativo' : 'Inativo'}
                color={cliente.ativo ? 'success' : 'default'}
              />
            </Box>
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary">
              Cliente desde
            </Typography>
            <Typography variant="body1">{new Date(cliente.createdAtUtc).toLocaleDateString('pt-BR')}</Typography>
          </Box>
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="h3" sx={{ mb: 1 }}>
          Propriedades ({cliente.propriedades.length})
        </Typography>
        {cliente.propriedades.length === 0 ? (
          <EmptyState
            icone={HomeWorkIcon}
            titulo="Nenhuma propriedade"
            descricao="Este cliente ainda não cadastrou nenhuma propriedade."
          />
        ) : (
          <List>
            {cliente.propriedades.map((propriedade) => (
              <ListItem
                key={propriedade.id}
                divider
                secondaryAction={
                  <Button
                    size="small"
                    variant="outlined"
                    startIcon={<LoginIcon />}
                    loading={impersonarMutation.isPending && impersonarMutation.variables === propriedade.id}
                    onClick={() => impersonarMutation.mutate(propriedade.id)}
                  >
                    Entrar como Cliente
                  </Button>
                }
              >
                <ListItemText primary={propriedade.nome} secondary={propriedade.tipo} />
              </ListItem>
            ))}
          </List>
        )}
      </Paper>
    </Box>
  );
}
