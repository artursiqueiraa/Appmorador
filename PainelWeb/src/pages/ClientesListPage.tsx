import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Typography,
  TextField,
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  TableContainer,
  Paper,
  Chip,
  Pagination,
  Skeleton,
  InputAdornment,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import PeopleIcon from '@mui/icons-material/People';
import { proprietariosService } from '../services/proprietariosService';
import { EmptyState } from '../components/EmptyState';
import { Breadcrumbs } from '../components/Breadcrumbs';

const TAMANHO_PAGINA = 20;

function useDebounce<T>(valor: T, atrasoMs: number): T {
  const [debounced, setDebounced] = useState(valor);
  useMemo(() => {
    const timer = setTimeout(() => setDebounced(valor), atrasoMs);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [valor]);
  return debounced;
}

/**
 * Sprint 22A (Fase 5) — leitura + busca (paginação server-side). Criar/Editar/Desativar cliente
 * não existem nesta Sprint: não há nenhum endpoint de gestão de conta de cliente (cliente só se
 * autocadastra via `/api/auth/register`) — construir esse CRUD completo seria uma Sprint própria,
 * fora do "mínimo necessário" combinado (ver ADR 0029).
 */
export function ClientesListPage() {
  const navigate = useNavigate();
  const [pagina, setPagina] = useState(1);
  const [busca, setBusca] = useState('');
  const buscaDebounced = useDebounce(busca, 300);

  const { data, isLoading } = useQuery({
    queryKey: ['proprietarios', pagina, buscaDebounced],
    queryFn: () => proprietariosService.listar(pagina, TAMANHO_PAGINA, buscaDebounced || undefined),
  });

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Breadcrumbs itens={[{ rotulo: 'Dashboard', rota: '/dashboard' }, { rotulo: 'Clientes' }]} />
      <Typography variant="h1">Clientes</Typography>

      <TextField
        placeholder="Buscar por nome ou e-mail"
        value={busca}
        onChange={(e) => {
          setBusca(e.target.value);
          setPagina(1);
        }}
        size="small"
        sx={{ maxWidth: 360 }}
        slotProps={{
          input: {
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon />
              </InputAdornment>
            ),
          },
        }}
      />

      {isLoading ? (
        <Skeleton variant="rounded" height={400} />
      ) : data && data.itens.length > 0 ? (
        <>
          <TableContainer component={Paper} variant="outlined">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Nome</TableCell>
                  <TableCell>E-mail</TableCell>
                  <TableCell>Propriedades</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Desde</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.itens.map((cliente) => (
                  <TableRow
                    key={cliente.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onClick={() => navigate(`/clientes/${cliente.id}`)}
                  >
                    <TableCell>{cliente.nome}</TableCell>
                    <TableCell>{cliente.email}</TableCell>
                    <TableCell>{cliente.quantidadePropriedades}</TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={cliente.ativo ? 'Ativo' : 'Inativo'}
                        color={cliente.ativo ? 'success' : 'default'}
                      />
                    </TableCell>
                    <TableCell>{new Date(cliente.createdAtUtc).toLocaleDateString('pt-BR')}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Pagination
            count={data.totalPaginas}
            page={pagina}
            onChange={(_, p) => setPagina(p)}
            sx={{ alignSelf: 'center' }}
          />
        </>
      ) : (
        <Paper variant="outlined">
          <EmptyState
            icone={PeopleIcon}
            titulo={busca ? 'Nenhum cliente encontrado' : 'Nenhum cliente ainda'}
            descricao={
              busca
                ? 'Tente buscar por outro nome ou e-mail.'
                : 'Clientes aparecem aqui assim que se cadastrarem no app.'
            }
          />
        </Paper>
      )}
    </Box>
  );
}
