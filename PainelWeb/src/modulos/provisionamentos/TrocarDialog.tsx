import { useMemo, useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Stack, Autocomplete, TextField, Typography } from '@mui/material';
import { useToastStore } from '../../stores/toastStore';
import { extrairMensagemErro } from '../../services/httpClient';
import { useProvisionamentosAtivosQuery } from './queries/useProvisionamentosAtivosQuery';
import { useEquipamentosParaAlocacaoQuery } from './queries/useEquipamentosParaAlocacaoQuery';
import { useTrocarMutation } from './mutations/useTrocarMutation';
import type { Vinculo } from './types';

interface TrocarDialogProps {
  vinculo: Vinculo | null;
  onFechar: () => void;
}

/**
 * Sprint 22B (ADR 0031) — trocar o equipamento de um vínculo ativo: encerra o antigo, cria um
 * novo (nunca edita em lugar, ver backend). O conteúdo só é montado quando há um `vinculo`
 * selecionado — o estado do formulário nasce limpo a cada montagem, sem precisar de um
 * `useEffect` para resetar (mesma regra de pureza de Hooks do `EquipamentoFormDialog`).
 */
export function TrocarDialog({ vinculo, onFechar }: TrocarDialogProps) {
  return (
    <Dialog open={Boolean(vinculo)} onClose={onFechar} maxWidth="sm" fullWidth>
      {vinculo ? <TrocarConteudo vinculo={vinculo} onFechar={onFechar} /> : null}
    </Dialog>
  );
}

function TrocarConteudo({ vinculo, onFechar }: { vinculo: Vinculo; onFechar: () => void }) {
  const mostrarToast = useToastStore((s) => s.mostrar);
  const [equipamentoNovoId, setEquipamentoNovoId] = useState<string | null>(null);
  const [observacoes, setObservacoes] = useState('');

  const { data: equipamentos, isFetching: carregandoEquipamentos } = useEquipamentosParaAlocacaoQuery(true);
  const { data: ativos, isFetching: carregandoAtivos } = useProvisionamentosAtivosQuery(1, 100);
  const trocarMutation = useTrocarMutation();

  const disponiveis = useMemo(() => {
    if (!equipamentos) return [];
    const idsProvisionados = new Set((ativos?.itens ?? []).map((v) => v.equipamentoId));
    return equipamentos.filter((e) => !idsProvisionados.has(e.id));
  }, [equipamentos, ativos]);

  function confirmar() {
    if (!equipamentoNovoId) return;

    trocarMutation.mutate(
      {
        propriedadeId: vinculo.propriedadeId,
        equipamentoAntigoId: vinculo.equipamentoId,
        equipamentoNovoId,
        observacoes: observacoes.trim() || undefined,
      },
      {
        onSuccess: () => {
          mostrarToast('Equipamento trocado.', 'success');
          onFechar();
        },
        onError: (erro) => mostrarToast(extrairMensagemErro(erro, 'Não foi possível trocar o equipamento.'), 'error'),
      },
    );
  }

  return (
    <>
      <DialogTitle>Trocar Equipamento</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body1">
            Propriedade: <strong>{vinculo.propriedadeNome}</strong>
            <br />
            Equipamento atual: <strong>{vinculo.equipamentoNome}</strong>
          </Typography>

          <Autocomplete
            options={disponiveis}
            loading={carregandoEquipamentos || carregandoAtivos}
            getOptionLabel={(e) => `${e.nome}${e.numeroSerie ? ` — ${e.numeroSerie}` : ''}`}
            onChange={(_, e) => setEquipamentoNovoId(e?.id ?? null)}
            renderInput={(params) => <TextField {...params} label="Novo equipamento" required />}
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
        <Button onClick={onFechar} disabled={trocarMutation.isPending}>
          Cancelar
        </Button>
        <Button variant="contained" onClick={confirmar} disabled={!equipamentoNovoId} loading={trocarMutation.isPending}>
          Trocar
        </Button>
      </DialogActions>
    </>
  );
}
