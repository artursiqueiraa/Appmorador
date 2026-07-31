import { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Stack,
  TextField,
  MenuItem,
  Grid,
  Alert,
} from '@mui/material';
import { useToastStore } from '../../stores/toastStore';
import { extrairMensagemErro } from '../../services/httpClient';
import { SeletorPropriedade } from '../../compartilhado/componentes/SeletorPropriedade';
import { useSalvarEquipamentoMutation } from './mutations/useSalvarEquipamentoMutation';
import type { EquipamentoAdmin, EstadoOperacionalEquipamento, FabricanteEquipamento } from './types';

const FABRICANTES: FabricanteEquipamento[] = ['Jfl', 'ControlId', 'Intelbras'];
const ROTULO_FABRICANTE: Record<FabricanteEquipamento, string> = {
  Jfl: 'JFL Active Bus',
  ControlId: 'Control iD',
  Intelbras: 'Intelbras',
};
const ESTADOS: EstadoOperacionalEquipamento[] = ['Ativo', 'EmManutencao', 'Inativo', 'Defeituoso'];
const ROTULO_ESTADO: Record<EstadoOperacionalEquipamento, string> = {
  Ativo: 'Ativo',
  EmManutencao: 'Em Manutenção',
  Inativo: 'Inativo',
  Defeituoso: 'Defeituoso',
};

interface EquipamentoFormDialogProps {
  aberto: boolean;
  equipamento: EquipamentoAdmin | null;
  onFechar: () => void;
}

function calcularFormInicial(equipamento: EquipamentoAdmin | null) {
  if (!equipamento) {
    return {
      propriedadeId: '',
      propriedadeNome: '',
      nome: '',
      fabricante: 'Jfl' as FabricanteEquipamento,
      numeroSerie: '',
      estadoOperacional: 'Ativo' as EstadoOperacionalEquipamento,
      ip: '',
      porta: '',
      usuario: '',
      senha: '',
      observacoes: '',
    };
  }

  return {
    propriedadeId: equipamento.propriedadeId,
    propriedadeNome: equipamento.propriedadeNome ?? '',
    nome: equipamento.nome,
    fabricante: equipamento.fabricante,
    numeroSerie: equipamento.numeroSerie ?? '',
    estadoOperacional: equipamento.estadoOperacional,
    ip: equipamento.ip ?? '',
    porta: equipamento.porta?.toString() ?? '',
    usuario: equipamento.usuario ?? '',
    senha: '',
    observacoes: equipamento.observacoes ?? '',
  };
}

/**
 * Sprint 22C.2 — cadastro/edição de Equipamento. Cada fabricante tem seu próprio método de
 * conexão (ver mission brief): o formulário exibe só os campos que aquele fabricante realmente
 * usa, nunca um formulário genérico com IP/Usuário/Senha forçados para todos. A lógica de
 * conectar/descobrir informações pertence ao backend (Providers) — este componente só monta os
 * campos certos e confia na validação do servidor.
 */
export function EquipamentoFormDialog({ aberto, equipamento, onFechar }: EquipamentoFormDialogProps) {
  return (
    <Dialog open={aberto} onClose={onFechar} maxWidth="sm" fullWidth>
      {aberto ? <EquipamentoFormConteudo equipamento={equipamento} onFechar={onFechar} /> : null}
    </Dialog>
  );
}

function EquipamentoFormConteudo({
  equipamento,
  onFechar,
}: {
  equipamento: EquipamentoAdmin | null;
  onFechar: () => void;
}) {
  const mostrarToast = useToastStore((s) => s.mostrar);
  const salvarMutation = useSalvarEquipamentoMutation();
  const [form, setForm] = useState(() => calcularFormInicial(equipamento));

  const ehJfl = form.fabricante === 'Jfl';
  const ehControlId = form.fabricante === 'ControlId';
  const ehIntelbras = form.fabricante === 'Intelbras';
  const exigeUsuario = ehControlId;
  const exigeConexao = ehControlId || ehIntelbras;

  const valido =
    Boolean(form.propriedadeId && form.nome.trim()) &&
    (ehJfl
      ? Boolean(form.numeroSerie.trim())
      : exigeConexao
        ? Boolean(form.ip.trim() && form.porta && (!exigeUsuario || form.usuario.trim()) && (equipamento || form.senha.trim()))
        : false);

  function salvar() {
    const camposComuns = {
      nome: form.nome.trim(),
      fabricante: form.fabricante,
      observacoes: form.observacoes.trim() || undefined,
    };

    const camposPorFabricante = ehJfl
      ? { numeroSerie: form.numeroSerie.trim() }
      : {
          ip: form.ip.trim(),
          porta: form.porta ? Number(form.porta) : undefined,
          usuario: exigeUsuario ? form.usuario.trim() : undefined,
          senha: form.senha.trim() || undefined,
        };

    const input = equipamento
      ? { modo: 'editar' as const, id: equipamento.id, request: { ...camposComuns, ...camposPorFabricante } }
      : {
          modo: 'criar' as const,
          request: {
            ...camposComuns,
            ...camposPorFabricante,
            propriedadeId: form.propriedadeId,
            estadoOperacional: form.estadoOperacional,
          },
        };

    salvarMutation.mutate(input, {
      onSuccess: () => {
        mostrarToast(equipamento ? 'Equipamento atualizado.' : 'Equipamento cadastrado.', 'success');
        onFechar();
      },
      onError: (erro) => mostrarToast(extrairMensagemErro(erro, 'Não foi possível salvar o equipamento.'), 'error'),
    });
  }

  return (
    <>
      <DialogTitle>{equipamento ? 'Editar Equipamento' : 'Novo Equipamento'}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {!equipamento ? (
            <SeletorPropriedade
              propriedadeId={form.propriedadeId || null}
              onChange={(propriedadeId, propriedadeNome) => setForm((f) => ({ ...f, propriedadeId, propriedadeNome }))}
            />
          ) : (
            <TextField label="Propriedade" value={form.propriedadeNome} disabled fullWidth />
          )}

          <TextField
            label="Nome amigável"
            value={form.nome}
            onChange={(e) => setForm((f) => ({ ...f, nome: e.target.value }))}
            required
            fullWidth
          />

          <Grid container spacing={2}>
            <Grid size={6}>
              <TextField
                select
                label="Fabricante"
                value={form.fabricante}
                onChange={(e) => setForm((f) => ({ ...f, fabricante: e.target.value as FabricanteEquipamento }))}
                disabled={Boolean(equipamento)}
                helperText={equipamento ? 'Fabricante não pode ser alterado após o cadastro' : undefined}
                fullWidth
              >
                {FABRICANTES.map((f) => (
                  <MenuItem key={f} value={f}>
                    {ROTULO_FABRICANTE[f]}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid size={6}>
              <TextField
                select
                label="Estado Operacional"
                value={form.estadoOperacional}
                onChange={(e) =>
                  setForm((f) => ({ ...f, estadoOperacional: e.target.value as EstadoOperacionalEquipamento }))
                }
                disabled={Boolean(equipamento)}
                helperText={equipamento ? 'Alterado na listagem' : undefined}
                fullWidth
              >
                {ESTADOS.map((estado) => (
                  <MenuItem key={estado} value={estado}>
                    {ROTULO_ESTADO[estado]}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          </Grid>

          {ehJfl ? (
            <>
              <TextField
                label="Número de Série"
                value={form.numeroSerie}
                onChange={(e) => setForm((f) => ({ ...f, numeroSerie: e.target.value }))}
                required
                fullWidth
              />
              <Alert severity="info">
                A central JFL disca para o AppMorador — não é preciso IP, Porta, Usuário ou Senha. Ao salvar, o
                cadastro fica aguardando a central conectar pelo Número de Série informado.
              </Alert>
            </>
          ) : (
            <>
              <Grid container spacing={2}>
                <Grid size={8}>
                  <TextField
                    label="Endereço IP"
                    value={form.ip}
                    onChange={(e) => setForm((f) => ({ ...f, ip: e.target.value }))}
                    required
                    fullWidth
                  />
                </Grid>
                <Grid size={4}>
                  <TextField
                    label="Porta"
                    type="number"
                    value={form.porta}
                    onChange={(e) => setForm((f) => ({ ...f, porta: e.target.value }))}
                    required
                    fullWidth
                  />
                </Grid>
              </Grid>

              <Grid container spacing={2}>
                {exigeUsuario ? (
                  <Grid size={6}>
                    <TextField
                      label="Usuário"
                      value={form.usuario}
                      onChange={(e) => setForm((f) => ({ ...f, usuario: e.target.value }))}
                      required
                      fullWidth
                    />
                  </Grid>
                ) : null}
                <Grid size={exigeUsuario ? 6 : 12}>
                  <TextField
                    label="Senha"
                    type="password"
                    value={form.senha}
                    onChange={(e) => setForm((f) => ({ ...f, senha: e.target.value }))}
                    required={!equipamento}
                    helperText={equipamento ? 'Deixe em branco para manter a senha atual' : undefined}
                    fullWidth
                  />
                </Grid>
              </Grid>

              <Alert severity="info">
                Ao salvar, o AppMorador conecta automaticamente no equipamento e atualiza o cadastro com o que
                conseguir descobrir.
              </Alert>
            </>
          )}

          <TextField
            label="Observações"
            value={form.observacoes}
            onChange={(e) => setForm((f) => ({ ...f, observacoes: e.target.value }))}
            multiline
            minRows={2}
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar} disabled={salvarMutation.isPending}>
          Cancelar
        </Button>
        <Button variant="contained" onClick={salvar} disabled={!valido} loading={salvarMutation.isPending}>
          Salvar
        </Button>
      </DialogActions>
    </>
  );
}
