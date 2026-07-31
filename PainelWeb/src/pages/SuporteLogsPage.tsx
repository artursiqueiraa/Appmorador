import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Typography,
  Paper,
  List,
  ListItem,
  ListItemText,
  Chip,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Skeleton,
} from '@mui/material';
import DescriptionIcon from '@mui/icons-material/Description';
import { auditoriaService } from '../services/auditoriaService';
import { EmptyState } from '../components/EmptyState';
import type { TipoAcaoAuditoria } from '../types/api';

const TODOS = 'Todos' as const;

/**
 * Sprint 22A (Fase 6) — logs de Auditoria (real, `GET /api/auditoria`). Filtro por tipo é
 * client-side (a lista já vem pronta) — "Operacional" (log distinto de auditoria, pedido pela
 * missão original) não tem nenhuma fonte cross-propriedade exposta hoje, fica fora desta Sprint
 * (ver ADR 0029/docs/painel/mapeamento-api.md).
 */
export function SuporteLogsPage() {
  const [filtroTipo, setFiltroTipo] = useState<TipoAcaoAuditoria | typeof TODOS>(TODOS);

  const { data: registros, isLoading } = useQuery({
    queryKey: ['auditoria-logs'],
    queryFn: () => auditoriaService.listarRecentes(100),
  });

  const filtrados = useMemo(
    () => (registros ?? []).filter((r) => filtroTipo === TODOS || r.acao === filtroTipo),
    [registros, filtroTipo],
  );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h1">Logs do Cliente</Typography>

      <FormControl size="small" sx={{ maxWidth: 260 }}>
        <InputLabel id="filtro-tipo-label">Tipo de ação</InputLabel>
        <Select
          labelId="filtro-tipo-label"
          label="Tipo de ação"
          value={filtroTipo}
          onChange={(e) => setFiltroTipo(e.target.value as TipoAcaoAuditoria | typeof TODOS)}
        >
          <MenuItem value={TODOS}>Todos</MenuItem>
          {(
            [
              'Login',
              'Logout',
              'ImpersonationInicio',
              'ImpersonationFim',
              'Criar',
              'Editar',
              'Excluir',
              'FalhaAutorizacao',
            ] as const
          ).map((tipo) => (
            <MenuItem key={tipo} value={tipo}>
              {tipo}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      {isLoading ? (
        <Skeleton variant="rounded" height={300} />
      ) : filtrados.length === 0 ? (
        <Paper variant="outlined">
          <EmptyState
            icone={DescriptionIcon}
            titulo="Nenhum log encontrado"
            descricao="Ajuste o filtro ou volte mais tarde."
          />
        </Paper>
      ) : (
        <Paper variant="outlined">
          <List>
            {filtrados.map((registro) => (
              <ListItem key={registro.id} divider>
                <ListItemText
                  primary={`${registro.usuarioNome} — ${registro.acao}`}
                  secondary={`${new Date(registro.dataHoraUtc).toLocaleString('pt-BR')}${registro.entidade ? ` · ${registro.entidade}` : ''}`}
                />
                {registro.ipAddress ? <Chip size="small" variant="outlined" label={registro.ipAddress} /> : null}
              </ListItem>
            ))}
          </List>
        </Paper>
      )}
    </Box>
  );
}
