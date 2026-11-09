import { useState, type FormEvent } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { FabricanteEquipamento } from '../../compartilhado/tipos/equipamento';
import { extrairMensagemErro } from '../../services/httpClient';
import { useCriarModeloMutation, useDefinirCapacidadesMutation } from './queries/useModelos';
import { CAPACIDADES, FABRICANTES, rotuloFabricante } from './rotulosModelos';
import type { CapacidadeEquipamento, ModeloEquipamento } from './types';

export function NovoModeloDialog({ aberto, onFechar }: { aberto: boolean; onFechar: (criado: boolean) => void }) {
  const criar = useCriarModeloMutation();
  const [fabricante, setFabricante] = useState<FabricanteEquipamento>('ControlId');
  const [nome, setNome] = useState('');

  const fechar = (criado: boolean) => {
    setNome('');
    criar.reset();
    onFechar(criado);
  };

  const enviar = (e: FormEvent) => {
    e.preventDefault();
    criar.mutate({ fabricante, nome: nome.trim() }, { onSuccess: () => fechar(true) });
  };

  return (
    <Dialog open={aberto} onClose={() => fechar(false)} fullWidth maxWidth="xs" component="form" onSubmit={enviar}>
      <DialogTitle>Novo modelo</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {criar.isError ? <Alert severity="error">{extrairMensagemErro(criar.error, 'Não foi possível criar o modelo.')}</Alert> : null}
          <TextField select label="Fabricante" value={fabricante} onChange={(e) => setFabricante(e.target.value as FabricanteEquipamento)} fullWidth>
            {FABRICANTES.map((f) => (
              <MenuItem key={f.valor} value={f.valor}>
                {f.rotulo}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Nome do modelo"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            required
            fullWidth
            placeholder="Ex.: iDFace Max"
            helperText="As capacidades são definidas depois, na lista."
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button onClick={() => fechar(false)} disabled={criar.isPending}>
          Cancelar
        </Button>
        <Button type="submit" variant="contained" loading={criar.isPending}>
          Criar modelo
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export function CapacidadesDialog({ modelo, onFechar }: { modelo: ModeloEquipamento | null; onFechar: (salvo: boolean) => void }) {
  const definir = useDefinirCapacidadesMutation();
  // Remontado por `key` (id do modelo) a cada abertura: o estado inicial vem sempre do modelo atual.
  const [marcadas, setMarcadas] = useState<CapacidadeEquipamento[]>(modelo?.capacidades ?? []);

  const alternar = (c: CapacidadeEquipamento) =>
    setMarcadas((atual) => (atual.includes(c) ? atual.filter((x) => x !== c) : [...atual, c]));

  const salvar = () => {
    if (!modelo) return;
    definir.mutate({ id: modelo.id, capacidades: marcadas }, { onSuccess: () => onFechar(true) });
  };

  return (
    <Dialog open={Boolean(modelo)} onClose={() => onFechar(false)} fullWidth maxWidth="sm">
      <DialogTitle>
        Capacidades de {modelo?.nome}
        <Typography variant="body2" color="text.secondary">
          {modelo ? `${rotuloFabricante(modelo.fabricante)}. Marque o que este modelo sabe fazer.` : ''}
        </Typography>
      </DialogTitle>
      <DialogContent>
        {definir.isError ? (
          <Alert severity="error" sx={{ mb: 2 }}>
            {extrairMensagemErro(definir.error, 'Não foi possível salvar.')}
          </Alert>
        ) : null}
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 0.5 }}>
          {CAPACIDADES.map((c) => (
            <FormControlLabel
              key={c.valor}
              control={<Checkbox checked={marcadas.includes(c.valor)} onChange={() => alternar(c.valor)} />}
              label={
                <Box>
                  <Typography>{c.rotulo}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {c.descricao}
                  </Typography>
                </Box>
              }
              sx={{ alignItems: 'flex-start', m: 0, py: 0.75, '& .MuiCheckbox-root': { pt: 0.25 } }}
            />
          ))}
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button onClick={() => onFechar(false)} disabled={definir.isPending}>
          Cancelar
        </Button>
        <Button variant="contained" onClick={salvar} loading={definir.isPending}>
          Salvar capacidades
        </Button>
      </DialogActions>
    </Dialog>
  );
}
