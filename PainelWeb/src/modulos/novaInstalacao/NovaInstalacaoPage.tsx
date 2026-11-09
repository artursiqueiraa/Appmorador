import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Box, Button, Paper, Step, StepLabel, Stepper, Typography } from '@mui/material';
import TaskAltOutlinedIcon from '@mui/icons-material/TaskAltOutlined';
import { extrairMensagemErro } from '../../services/httpClient';
import { novaInstalacaoAdaptador } from './adaptadores/novaInstalacaoAdaptador';
import { EtapaCliente, EtapaEquipamento, EtapaPropriedade, EtapaRevisao } from './EtapasNovaInstalacao';
import { etapaClienteValida, etapaPropriedadeValida, type DadosInstalacao } from './validacaoInstalacao';
import type { NovaInstalacaoResponse } from './types';

const ETAPAS = [
  { titulo: 'Cliente', descricao: 'Quem vai usar o sistema. Estes dados viram o login do cliente no app.' },
  { titulo: 'Propriedade', descricao: 'O primeiro lugar protegido deste cliente.' },
  { titulo: 'Equipamento', descricao: 'Vincule um equipamento já cadastrado e livre, ou deixe para depois.' },
  { titulo: 'Revisão', descricao: 'Confira antes de criar. Nada é salvo até você confirmar.' },
];

const VAZIO: DadosInstalacao = {
  nomeCliente: '',
  emailCliente: '',
  senhaInicial: '',
  nomePropriedade: '',
  tipoPropriedade: '',
  enderecoPropriedade: '',
  equipamentoId: '',
};

/**
 * Akteck Seg (ADR 0035) — "Nova instalação": cliente + propriedade + equipamento num envio
 * só (`POST api/painel/novas-instalacoes`). Nada é gravado até a revisão. O backend valida
 * e-mail e equipamento antes de criar qualquer registro.
 */
export function NovaInstalacaoPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [etapa, setEtapa] = useState(0);
  const [dados, setDados] = useState<DadosInstalacao>(VAZIO);
  const [mostrarErros, setMostrarErros] = useState(false);
  const [concluida, setConcluida] = useState<NovaInstalacaoResponse | null>(null);

  const equipamentos = useQuery({
    queryKey: ['nova-instalacao', 'equipamentos-livres'],
    queryFn: novaInstalacaoAdaptador.listarEquipamentosLivres,
  });

  const criar = useMutation({
    mutationFn: novaInstalacaoAdaptador.criar,
    onSuccess: (resposta) => {
      setConcluida(resposta);
      void queryClient.invalidateQueries();
    },
  });

  const alterar = <K extends keyof DadosInstalacao>(campo: K, valor: DadosInstalacao[K]) => setDados((d) => ({ ...d, [campo]: valor }));

  const podeAvancar = etapa === 0 ? etapaClienteValida(dados) : etapa === 1 ? etapaPropriedadeValida(dados) : true;

  const avancar = () => {
    if (!podeAvancar) {
      setMostrarErros(true);
      return;
    }
    setMostrarErros(false);
    if (etapa < ETAPAS.length - 1) {
      setEtapa(etapa + 1);
      return;
    }
    criar.mutate({
      nomeCliente: dados.nomeCliente.trim(),
      emailCliente: dados.emailCliente.trim(),
      senhaInicial: dados.senhaInicial,
      nomePropriedade: dados.nomePropriedade.trim(),
      tipoPropriedade: dados.tipoPropriedade as Exclude<DadosInstalacao['tipoPropriedade'], ''>,
      enderecoPropriedade: dados.enderecoPropriedade.trim() || undefined,
      equipamentoId: dados.equipamentoId || undefined,
    });
  };

  const recomecar = () => {
    setDados(VAZIO);
    setEtapa(0);
    setConcluida(null);
    criar.reset();
  };

  if (concluida) {
    return (
      <Box sx={{ maxWidth: 640, mx: 'auto' }}>
        <Paper variant="outlined" sx={{ p: { xs: 3, sm: 4 }, display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TaskAltOutlinedIcon color="success" sx={{ fontSize: 40 }} />
          <Typography variant="h1">Instalação criada</Typography>
          <Typography color="text.secondary">
            {dados.nomeCliente.trim()} já pode entrar no app com o e-mail {dados.emailCliente.trim()}. A propriedade{' '}
            {concluida.nomePropriedade} foi criada
            {dados.equipamentoId
              ? concluida.vinculoEquipamentoId
                ? ' com o equipamento vinculado.'
                : ', mas o equipamento não pôde ser vinculado agora. Vincule em Provisionamentos.'
              : ' sem equipamento.'}
          </Typography>
          <Box sx={{ display: 'flex', gap: 1.5, flexWrap: 'wrap', mt: 1 }}>
            <Button variant="contained" onClick={() => navigate(`/clientes/${concluida.clienteId}`)}>
              Abrir cliente
            </Button>
            <Button variant="outlined" onClick={recomecar}>
              Nova instalação
            </Button>
          </Box>
        </Paper>
      </Box>
    );
  }

  return (
    <Box sx={{ maxWidth: 760, mx: 'auto', display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Box>
        <Typography variant="h1">Nova instalação</Typography>
        <Typography color="text.secondary">Cadastre um cliente novo, a casa ou negócio dele e o equipamento, num lugar só.</Typography>
      </Box>

      <Stepper activeStep={etapa} alternativeLabel>
        {ETAPAS.map((e) => (
          <Step key={e.titulo}>
            <StepLabel>{e.titulo}</StepLabel>
          </Step>
        ))}
      </Stepper>

      <Paper variant="outlined" sx={{ p: { xs: 2.5, sm: 3.5 } }}>
        <Typography variant="h2" component="h2">
          {ETAPAS[etapa].titulo}
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 3 }}>
          {ETAPAS[etapa].descricao}
        </Typography>

        {etapa === 0 ? <EtapaCliente dados={dados} alterar={alterar} mostrarErros={mostrarErros} /> : null}
        {etapa === 1 ? <EtapaPropriedade dados={dados} alterar={alterar} mostrarErros={mostrarErros} /> : null}
        {etapa === 2 ? (
          <EtapaEquipamento
            dados={dados}
            alterar={alterar}
            equipamentos={equipamentos.data ?? []}
            carregando={equipamentos.isLoading}
            erro={equipamentos.isError}
            onTentarDeNovo={() => void equipamentos.refetch()}
          />
        ) : null}
        {etapa === 3 ? <EtapaRevisao dados={dados} equipamentos={equipamentos.data ?? []} /> : null}

        {criar.isError ? (
          <Alert severity="error" sx={{ mt: 3 }}>
            {extrairMensagemErro(criar.error, 'Não foi possível criar a instalação.')}
          </Alert>
        ) : null}

        <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, mt: 4 }}>
          <Button variant="outlined" onClick={() => (etapa === 0 ? navigate(-1) : setEtapa(etapa - 1))} disabled={criar.isPending}>
            {etapa === 0 ? 'Cancelar' : 'Voltar'}
          </Button>
          <Button variant="contained" onClick={avancar} loading={criar.isPending}>
            {etapa === ETAPAS.length - 1 ? 'Criar instalação' : 'Continuar'}
          </Button>
        </Box>
      </Paper>
    </Box>
  );
}
