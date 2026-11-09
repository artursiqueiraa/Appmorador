import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Alert, Box, Button, Chip, Paper, Skeleton, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import TaskAltOutlinedIcon from '@mui/icons-material/TaskAltOutlined';
import { useDiagnosticoEquipamentosQuery } from '../diagnostico/queries/useDiagnosticoEquipamentosQuery';
import { agruparPorPropriedade, type PropriedadeEmAtencao } from './agruparAtencao';
import { formatarDataHora } from './rotulosDashboard';

/** Maior página que o backend aceita (`DiagnosticoServico`: > 100 cai para 20). */
const TAMANHO_AMOSTRA = 100;


function Motivos({ p }: { p: PropriedadeEmAtencao }) {
  return (
    <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'wrap' }}>
      {p.offline > 0 ? <Chip size="small" color="error" variant="outlined" label={`${p.offline} offline`} /> : null}
      {p.comDefeito > 0 ? <Chip size="small" color="warning" variant="outlined" label={`${p.comDefeito} com defeito`} /> : null}
      {p.comProblema > 0 ? <Chip size="small" color="warning" variant="outlined" label={`${p.comProblema} com alerta`} /> : null}
    </Box>
  );
}

/**
 * Akteck Seg (ADR 0035) — a fila de trabalho do Dashboard. Usa o mesmo endpoint de
 * Diagnóstico (só Master/Técnico podem ler — para o Suporte o bloco não é montado).
 * Mostra no máximo 100 equipamentos por amostra e diz isso quando a base é maior.
 */
export function ClientesAtencao() {
  const navigate = useNavigate();
  const { data, isLoading, isError, refetch } = useDiagnosticoEquipamentosQuery(1, TAMANHO_AMOSTRA, 60_000);
  const fila = useMemo(() => agruparPorPropriedade(data?.itens ?? []), [data]);
  const amostraParcial = (data?.totalItens ?? 0) > TAMANHO_AMOSTRA;

  return (
    <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, px: 2.5, py: 2 }}>
        <Box>
          <Typography variant="h3" component="h2">
            Clientes que precisam de atenção
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Propriedades com equipamento offline, com defeito ou com alerta ativo.
          </Typography>
        </Box>
        {fila.length > 0 ? (
          <Button variant="outlined" onClick={() => navigate('/diagnostico-equipamentos')}>
            Ver diagnóstico completo
          </Button>
        ) : null}
      </Box>

      {isLoading ? (
        <Box sx={{ px: 2.5, pb: 2.5, display: 'flex', flexDirection: 'column', gap: 1 }}>
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} variant="rounded" height={44} />
          ))}
        </Box>
      ) : isError ? (
        <Box sx={{ px: 2.5, pb: 2.5 }}>
          <Alert severity="error" action={<Button color="inherit" onClick={() => void refetch()}>Tentar de novo</Button>}>
            Não foi possível carregar o estado dos equipamentos agora.
          </Alert>
        </Box>
      ) : fila.length === 0 ? (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, px: 2.5, pb: 2.5 }}>
          <TaskAltOutlinedIcon color="success" />
          <Typography color="text.secondary">Nenhum cliente precisa de atenção agora. Todos os equipamentos estão respondendo.</Typography>
        </Box>
      ) : (
        <>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Propriedade</TableCell>
                <TableCell>Motivo</TableCell>
                <TableCell>Último sinal</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {fila.slice(0, 8).map((p) => (
                <TableRow key={p.propriedadeId} hover>
                  <TableCell sx={{ fontWeight: 500 }}>{p.propriedadeNome}</TableCell>
                  <TableCell>
                    <Motivos p={p} />
                  </TableCell>
                  <TableCell sx={{ color: 'text.secondary', whiteSpace: 'nowrap' }}>
                    {p.ultimoSinalUtc ? formatarDataHora(p.ultimoSinalUtc) : 'Nunca respondeu'}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          {fila.length > 8 || amostraParcial ? (
            <Typography variant="caption" color="text.secondary" component="p" sx={{ px: 2.5, py: 1.5 }}>
              {fila.length > 8 ? `Mostrando 8 de ${fila.length} propriedades. ` : ''}
              {amostraParcial ? `Análise baseada nos primeiros ${TAMANHO_AMOSTRA} equipamentos.` : ''}
            </Typography>
          ) : null}
        </>
      )}
    </Paper>
  );
}
