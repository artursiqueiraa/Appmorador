import { useMemo, useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Stack, Autocomplete, TextField } from '@mui/material';
import { SeletorPropriedade } from '../../compartilhado/componentes/SeletorPropriedade';
import { useToastStore } from '../../stores/toastStore';
import { extrairMensagemErro } from '../../services/httpClient';
import { useProvisionamentosAtivosQuery } from './queries/useProvisionamentosAtivosQuery';
import { useEquipamentosParaAlocacaoQuery } from './queries/useEquipamentosParaAlocacaoQuery';
import { useProvisionarMutation } from './mutations/useProvisionarMutation';

interface ProvisionarDialogProps {
  aberto: boolean;
  onFechar: () => void;
}

/**
 * Sprint 22B (ADR 0031) — wizard de ativação: escolher Propriedade + Equipamento disponível
 * (sem vínculo ativo em nenhum lugar). "Disponível" é calculado no cliente cruzando a lista de
 * equipamentos com a lista de vínculos ativos — não existe (nem é necessário) um endpoint de
 * "equipamentos disponíveis" dedicado nesta escala.
 */
export function ProvisionarDialog({ aberto, onFechar }: ProvisionarDialogProps) {
  const mostrarToast = useToastStore((s) => s.mostrar);
  const [propriedadeId, setPropriedadeId] = useState<string | null>(null);
  const [equipamentoId, setEquipamentoId] = useState<string | null>(null);
  const [observacoes, setObservacoes] = useState('');

  const { data: equipamentos, isFetching: carregandoEquipamentos } = useEquipamentosParaAlocacaoQuery(aberto);
  const { data: ativos, isFetching: carregandoAtivos } = useProvisionamentosAtivosQuery(1, 100);
  const provisionarMutation = useProvisionarMutation();

  const disponiveis = useMemo(() => {
    if (!equipamentos) return [];
    const idsProvisionados = new Set((ativos?.itens ?? []).map((v) => v.equipamentoId));
    return equipamentos.filter((e) => !idsProvisionados.has(e.id));
  }, [equipamentos, ativos]);

  function limparEFechar() {
    setPropriedadeId(null);
    setEquipamentoId(null);
    setObservacoes('');
    onFechar();
  }

  function confirmar() {
    if (!propriedadeId || !equipamentoId) return;

    provisionarMutation.mutate(
      { propriedadeId, equipamentoId, observacoes: observacoes.trim() || undefined },
      {
        onSuccess: () => {
          mostrarToast('Equipamento provisionado.', 'success');
          limparEFechar();
        },
        onError: (erro) => mostrarToast(extrairMensagemErro(erro, 'Não foi possível provisionar o equipamento.'), 'error'),
      },
    );
  }

  return (
    <Dialog open={aberto} onClose={limparEFechar} maxWidth="sm" fullWidth>
      <DialogTitle>Provisionar Equipamento</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <SeletorPropriedade propriedadeId={propriedadeId} onChange={(id) => setPropriedadeId(id)} />

          <Autocomplete
            options={disponiveis}
            loading={carregandoEquipamentos || carregandoAtivos}
            getOptionLabel={(e) => `${e.nome}${e.numeroSerie ? ` — ${e.numeroSerie}` : ''}`}
            onChange={(_, e) => setEquipamentoId(e?.id ?? null)}
            renderInput={(params) => <TextField {...params} label="Equipamento disponível" required />}
            noOptionsText="Nenhum equipamento disponível no momento"
          />

          <TextField
            label="Observações"
            value={observacoes}
            onChange={(e) => setObservacoes(e.target.value)}
            multiline
            minRows={2}
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={limparEFechar} disabled={provisionarMutation.isPending}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          onClick={confirmar}
          disabled={!propriedadeId || !equipamentoId}
          loading={provisionarMutation.isPending}
        >
          Provisionar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
