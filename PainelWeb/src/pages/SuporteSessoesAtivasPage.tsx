import { useEffect, useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, Box, Chip, List, ListItem, ListItemText, Paper, Skeleton, Typography } from '@mui/material';
import HistoryIcon from '@mui/icons-material/History';
import { auditoriaService } from '../services/auditoriaService';
import { EmptyState } from '../components/EmptyState';

const DURACAO_IMPERSONATION_MS = 15 * 60 * 1000;

/**
 * Sprint 22A (Fase 6) — "Sessões Ativas" sem endpoint próprio (decisão explícita do usuário, ver
 * ADR 0029): impersonation é 100% stateless (JWT auto-contido), então não existe nenhum registro
 * de "sessão ativa" persistido — a tela infere isso a partir do log de auditoria (um
 * `ImpersonationInicio` sem `ImpersonationFim` correspondente, dentro dos 15 minutos de vida do
 * token, aparece como "ativa agora"). **Sem botão de forçar logout** — revogar um token já
 * emitido exigiria um mecanismo que não existe hoje (ver ARQUITETURA_ATUAL.md).
 *
 * "Agora" nunca é lido via `Date.now()` direto no corpo de render/useMemo (regra de pureza dos
 * Hooks) — vem de um estado próprio (`agoraMs`), atualizado por um efeito a cada 1s.
 */
export function SuporteSessoesAtivasPage() {
  const [agoraMs, setAgoraMs] = useState(() => Date.now());

  useEffect(() => {
    const intervalo = setInterval(() => setAgoraMs(Date.now()), 1000);
    return () => clearInterval(intervalo);
  }, []);

  const { data: registros, isLoading } = useQuery({
    queryKey: ['auditoria-sessoes'],
    queryFn: () => auditoriaService.listarRecentes(100),
    refetchInterval: 30_000,
  });

  const sessoesAtivas = useMemo(() => {
    if (!registros) return [];

    const inicios = registros.filter((r) => r.acao === 'ImpersonationInicio');
    const fins = new Set(registros.filter((r) => r.acao === 'ImpersonationFim').map((r) => r.entidadeId));

    return inicios.filter((inicio) => {
      const jaEncerrada = fins.has(inicio.entidadeId);
      const expirou = agoraMs - new Date(inicio.dataHoraUtc).getTime() > DURACAO_IMPERSONATION_MS;
      return !jaEncerrada && !expirou;
    });
  }, [registros, agoraMs]);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h1">Sessões Ativas</Typography>
      <Alert severity="info">
        Inferido a partir do log de auditoria (atualiza a cada 30s) — não há revogação de token em tempo real nesta
        Sprint.
      </Alert>

      {isLoading ? (
        <Skeleton variant="rounded" height={200} />
      ) : sessoesAtivas.length === 0 ? (
        <Paper variant="outlined">
          <EmptyState
            icone={HistoryIcon}
            titulo="Nenhuma sessão ativa agora"
            descricao="Nenhuma impersonation em andamento nos últimos 15 minutos."
          />
        </Paper>
      ) : (
        <Paper variant="outlined">
          <List>
            {sessoesAtivas.map((sessao) => {
              const minutosRestantes = Math.max(
                0,
                Math.round((DURACAO_IMPERSONATION_MS - (agoraMs - new Date(sessao.dataHoraUtc).getTime())) / 60000),
              );
              return (
                <ListItem key={sessao.id} divider>
                  <ListItemText
                    primary={`${sessao.usuarioNome} — propriedade ${sessao.entidadeId}`}
                    secondary={`Iniciada em ${new Date(sessao.dataHoraUtc).toLocaleString('pt-BR')}`}
                  />
                  <Chip label={`~${minutosRestantes} min restantes`} color="warning" size="small" />
                </ListItem>
              );
            })}
          </List>
        </Paper>
      )}
    </Box>
  );
}
