import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Autocomplete, TextField, Stack } from '@mui/material';
import { proprietariosService } from '../../services/proprietariosService';
import { useDebounce } from '../hooks/useDebounce';

interface SeletorPropriedadeProps {
  propriedadeId: string | null;
  onChange: (propriedadeId: string, propriedadeNome: string) => void;
  disabled?: boolean;
}

/**
 * Sprint 22B (ADR 0031) — não existe (nem foi criado nesta Sprint) um endpoint que liste todas as
 * Propriedades da plataforma cross-cliente; o que já existe é `GET /api/proprietarios` (busca) +
 * `GET /api/proprietarios/{id}` (detalhe com as propriedades daquele cliente). Este seletor
 * cascateia os dois: busca o cliente, depois escolhe uma das propriedades dele — 100% reuso de
 * endpoints já existentes, sem precisar de um endpoint novo só para popular um combobox.
 */
export function SeletorPropriedade({ propriedadeId, onChange, disabled }: SeletorPropriedadeProps) {
  const [buscaCliente, setBuscaCliente] = useState('');
  const buscaDebounced = useDebounce(buscaCliente, 300);
  const [clienteId, setClienteId] = useState<string | null>(null);

  const { data: clientes, isFetching: buscandoClientes } = useQuery({
    queryKey: ['seletor-propriedade-clientes', buscaDebounced],
    queryFn: () => proprietariosService.listar(1, 10, buscaDebounced || undefined),
    enabled: buscaDebounced.length >= 2,
  });

  const { data: clienteDetalhe, isFetching: buscandoPropriedades } = useQuery({
    queryKey: ['seletor-propriedade-detalhe', clienteId],
    queryFn: () => proprietariosService.obterDetalhe(clienteId!),
    enabled: Boolean(clienteId),
  });

  const propriedadeAtual = clienteDetalhe?.propriedades.find((p) => p.id === propriedadeId) ?? null;

  return (
    <Stack spacing={2}>
      <Autocomplete
        disabled={disabled}
        options={clientes?.itens ?? []}
        loading={buscandoClientes}
        getOptionLabel={(cliente) => `${cliente.nome} (${cliente.email})`}
        filterOptions={(x) => x}
        onInputChange={(_, valor) => setBuscaCliente(valor)}
        onChange={(_, cliente) => setClienteId(cliente?.id ?? null)}
        renderInput={(params) => (
          <TextField {...params} label="Buscar cliente por nome ou e-mail" placeholder="Digite ao menos 2 letras" />
        )}
      />
      <Autocomplete
        disabled={disabled || !clienteId}
        options={clienteDetalhe?.propriedades ?? []}
        loading={buscandoPropriedades}
        value={propriedadeAtual}
        getOptionLabel={(propriedade) => `${propriedade.nome} (${propriedade.tipo})`}
        isOptionEqualToValue={(a, b) => a.id === b.id}
        onChange={(_, propriedade) => propriedade && onChange(propriedade.id, propriedade.nome)}
        renderInput={(params) => <TextField {...params} label="Propriedade" required />}
      />
    </Stack>
  );
}
