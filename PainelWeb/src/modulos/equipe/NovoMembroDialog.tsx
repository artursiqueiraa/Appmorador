import { useState, type FormEvent } from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField } from '@mui/material';
import { extrairMensagemErro } from '../../services/httpClient';
import type { RoleSistema } from '../../types/api';
import { useCriarMembroMutation } from './queries/useEquipe';
import { DESCRICAO_PAPEL, ROTULO_PAPEL } from './rotulosEquipe';

const PAPEIS: RoleSistema[] = ['Suporte', 'Tecnico', 'Master'];

/** Akteck Seg (ADR 0035) — criação de membro da equipe. A senha inicial segue a mesma regra do cadastro de clientes. */
export function NovoMembroDialog({ aberto, onFechar }: { aberto: boolean; onFechar: (criado: boolean) => void }) {
  const criar = useCriarMembroMutation();
  const [nome, setNome] = useState('');
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [papel, setPapel] = useState<RoleSistema>('Suporte');

  const fechar = (criado: boolean) => {
    setNome('');
    setEmail('');
    setSenha('');
    setPapel('Suporte');
    criar.reset();
    onFechar(criado);
  };

  const enviar = (e: FormEvent) => {
    e.preventDefault();
    criar.mutate({ nome: nome.trim(), email: email.trim(), senha, roleGlobal: papel }, { onSuccess: () => fechar(true) });
  };

  return (
    <Dialog open={aberto} onClose={() => fechar(false)} fullWidth maxWidth="xs" component="form" onSubmit={enviar}>
      <DialogTitle>Novo membro da equipe</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {criar.isError ? <Alert severity="error">{extrairMensagemErro(criar.error, 'Não foi possível criar o membro.')}</Alert> : null}
          <TextField label="Nome" value={nome} onChange={(e) => setNome(e.target.value)} required autoFocus fullWidth />
          <TextField label="E-mail" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required fullWidth />
          <TextField
            label="Senha inicial"
            type="password"
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            required
            fullWidth
            // Mesma regra de `RegisterRequest` (backend). `CriarUsuarioInternoRequest` ainda não valida
            // a senha no servidor (DIVIDA_TECNICA 54) — por ora a regra só é garantida aqui.
            slotProps={{ htmlInput: { pattern: '^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).{8,}$', title: 'Mínimo de 8 caracteres, com maiúscula, minúscula e número.' } }}
            helperText="Mínimo de 8 caracteres, com maiúscula, minúscula e número. Peça para a pessoa trocar no primeiro acesso."
          />
          <TextField
            select
            label="Papel"
            value={papel}
            onChange={(e) => setPapel(e.target.value as RoleSistema)}
            fullWidth
            helperText={DESCRICAO_PAPEL[papel]}
          >
            {PAPEIS.map((p) => (
              <MenuItem key={p} value={p}>
                {ROTULO_PAPEL[p]}
              </MenuItem>
            ))}
          </TextField>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button onClick={() => fechar(false)} disabled={criar.isPending}>
          Cancelar
        </Button>
        <Button type="submit" variant="contained" loading={criar.isPending}>
          Criar membro
        </Button>
      </DialogActions>
    </Dialog>
  );
}
