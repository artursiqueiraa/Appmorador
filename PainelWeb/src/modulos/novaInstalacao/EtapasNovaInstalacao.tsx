import { Alert, Box, Button, MenuItem, Radio, Skeleton, Stack, TextField, Typography } from '@mui/material';
import { TIPOS_PROPRIEDADE, ROTULO_TIPO_PROPRIEDADE, type TipoPropriedade } from '../../compartilhado/tipos/propriedade';
import { REGRA_EMAIL, REGRA_SENHA, type Alterar, type DadosInstalacao } from './validacaoInstalacao';
import type { EquipamentoLivre } from './types';

export function EtapaCliente({ dados, alterar, mostrarErros }: { dados: DadosInstalacao; alterar: Alterar; mostrarErros: boolean }) {
  const emailInvalido = mostrarErros && !REGRA_EMAIL.test(dados.emailCliente.trim());
  const senhaInvalida = mostrarErros && !REGRA_SENHA.test(dados.senhaInicial);
  return (
    <Stack spacing={2.5}>
      <TextField
        label="Nome do cliente"
        value={dados.nomeCliente}
        onChange={(e) => alterar('nomeCliente', e.target.value)}
        error={mostrarErros && !dados.nomeCliente.trim()}
        helperText={mostrarErros && !dados.nomeCliente.trim() ? 'Informe o nome.' : 'Como o cliente vai aparecer no app e no painel.'}
        required
        autoFocus
        fullWidth
      />
      <TextField
        label="E-mail do cliente"
        type="email"
        value={dados.emailCliente}
        onChange={(e) => alterar('emailCliente', e.target.value)}
        error={emailInvalido}
        helperText={emailInvalido ? 'Informe um e-mail válido.' : 'É o login do cliente no app.'}
        required
        fullWidth
      />
      <TextField
        label="Senha inicial"
        type="password"
        value={dados.senhaInicial}
        onChange={(e) => alterar('senhaInicial', e.target.value)}
        error={senhaInvalida}
        helperText="Mínimo de 8 caracteres, com maiúscula, minúscula e número. Entregue ao cliente por um canal seguro."
        required
        fullWidth
      />
    </Stack>
  );
}

export function EtapaPropriedade({ dados, alterar, mostrarErros }: { dados: DadosInstalacao; alterar: Alterar; mostrarErros: boolean }) {
  return (
    <Stack spacing={2.5}>
      <TextField
        label="Nome da propriedade"
        value={dados.nomePropriedade}
        onChange={(e) => alterar('nomePropriedade', e.target.value)}
        error={mostrarErros && !dados.nomePropriedade.trim()}
        helperText='Um nome fácil de reconhecer, como "Casa" ou "Loja Centro".'
        required
        autoFocus
        fullWidth
      />
      <TextField
        select
        label="Tipo"
        value={dados.tipoPropriedade}
        onChange={(e) => alterar('tipoPropriedade', e.target.value as TipoPropriedade)}
        error={mostrarErros && dados.tipoPropriedade === ''}
        helperText={TIPOS_PROPRIEDADE.find((t) => t.valor === dados.tipoPropriedade)?.descricao ?? 'Escolha o tipo de propriedade.'}
        required
        fullWidth
      >
        {TIPOS_PROPRIEDADE.map((t) => (
          <MenuItem key={t.valor} value={t.valor}>
            {t.rotulo}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        label="Endereço (opcional)"
        value={dados.enderecoPropriedade}
        onChange={(e) => alterar('enderecoPropriedade', e.target.value)}
        placeholder="Rua, número, bairro, cidade"
        fullWidth
      />
    </Stack>
  );
}

export function EtapaEquipamento({
  dados,
  alterar,
  equipamentos,
  carregando,
  erro,
  onTentarDeNovo,
}: {
  dados: DadosInstalacao;
  alterar: Alterar;
  equipamentos: EquipamentoLivre[];
  carregando: boolean;
  erro: boolean;
  onTentarDeNovo: () => void;
}) {
  const opcoes: { id: string; titulo: string; detalhe: string }[] = [
    { id: '', titulo: 'Sem equipamento por enquanto', detalhe: 'Você pode vincular depois, em Provisionamentos.' },
    ...equipamentos.map((e) => ({
      id: e.id,
      titulo: e.nome,
      detalhe: [e.fabricante === 'ControlId' ? 'Control iD' : e.fabricante === 'Jfl' ? 'JFL' : e.fabricante, e.numeroSerie ? `série ${e.numeroSerie}` : null]
        .filter(Boolean)
        .join(', '),
    })),
  ];

  if (carregando) {
    return (
      <Stack spacing={1}>
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} variant="rounded" height={56} />
        ))}
      </Stack>
    );
  }

  return (
    <Stack spacing={1} role="radiogroup" aria-label="Equipamento da instalação">
      {erro ? (
        <Alert severity="warning" action={<Button color="inherit" onClick={onTentarDeNovo}>Tentar de novo</Button>}>
          Não foi possível carregar os equipamentos livres. Você ainda pode seguir sem equipamento.
        </Alert>
      ) : equipamentos.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Nenhum equipamento livre agora. Cadastre um em Equipamentos ou siga sem equipamento.
        </Typography>
      ) : null}
      {opcoes.map((o) => {
        const marcado = dados.equipamentoId === o.id;
        return (
          <Box
            key={o.id || 'nenhum'}
            component="label"
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1.5,
              p: 1.5,
              borderRadius: 2,
              border: 1,
              borderColor: marcado ? 'primary.main' : 'divider',
              bgcolor: marcado ? 'action.selected' : 'transparent',
              cursor: 'pointer',
            }}
          >
            <Radio checked={marcado} onChange={() => alterar('equipamentoId', o.id)} value={o.id} />
            <Box sx={{ minWidth: 0 }}>
              <Typography sx={{ fontWeight: 500 }}>{o.titulo}</Typography>
              <Typography variant="body2" color="text.secondary">
                {o.detalhe}
              </Typography>
            </Box>
          </Box>
        );
      })}
    </Stack>
  );
}

function Linha({ rotulo, valor }: { rotulo: string; valor: string }) {
  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '180px minmax(0, 1fr)' }, gap: 0.5, py: 1.25, borderTop: 1, borderColor: 'divider' }}>
      <Typography component="dt" color="text.secondary">
        {rotulo}
      </Typography>
      <Typography component="dd" sx={{ m: 0, overflowWrap: 'anywhere' }}>
        {valor}
      </Typography>
    </Box>
  );
}

export function EtapaRevisao({ dados, equipamentos }: { dados: DadosInstalacao; equipamentos: EquipamentoLivre[] }) {
  const equipamento = equipamentos.find((e) => e.id === dados.equipamentoId);
  return (
    <Box component="dl" sx={{ m: 0 }}>
      <Linha rotulo="Cliente" valor={dados.nomeCliente.trim()} />
      <Linha rotulo="E-mail (login)" valor={dados.emailCliente.trim()} />
      <Linha rotulo="Propriedade" valor={dados.nomePropriedade.trim()} />
      <Linha rotulo="Tipo" valor={dados.tipoPropriedade ? ROTULO_TIPO_PROPRIEDADE[dados.tipoPropriedade] : ''} />
      <Linha rotulo="Endereço" valor={dados.enderecoPropriedade.trim() || 'Não informado'} />
      <Linha rotulo="Equipamento" valor={equipamento ? equipamento.nome : 'Sem equipamento por enquanto'} />
    </Box>
  );
}
