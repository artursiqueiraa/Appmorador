import { Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle } from '@mui/material';

interface ConfirmDialogProps {
  aberto: boolean;
  titulo: string;
  mensagem: string;
  destrutivo?: boolean;
  carregando?: boolean;
  onConfirmar: () => void;
  onCancelar: () => void;
}

/** Sprint 22A (Fase 7) — toda ação destrutiva/sensível (desativar, encerrar sessão de outro usuário) passa por aqui, nunca executa direto. */
export function ConfirmDialog({
  aberto,
  titulo,
  mensagem,
  destrutivo,
  carregando,
  onConfirmar,
  onCancelar,
}: ConfirmDialogProps) {
  return (
    <Dialog open={aberto} onClose={onCancelar}>
      <DialogTitle>{titulo}</DialogTitle>
      <DialogContent>
        <DialogContentText>{mensagem}</DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancelar} disabled={carregando}>
          Cancelar
        </Button>
        <Button onClick={onConfirmar} color={destrutivo ? 'error' : 'primary'} variant="contained" loading={carregando}>
          Confirmar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
