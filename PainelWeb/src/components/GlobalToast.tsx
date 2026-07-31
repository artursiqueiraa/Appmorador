import { Alert, Snackbar } from '@mui/material';
import { useToastStore } from '../stores/toastStore';

export function GlobalToast() {
  const { aberto, mensagem, severidade, fechar } = useToastStore();

  return (
    <Snackbar
      open={aberto}
      autoHideDuration={5000}
      onClose={fechar}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
    >
      <Alert onClose={fechar} severity={severidade} variant="filled" sx={{ width: '100%' }}>
        {mensagem}
      </Alert>
    </Snackbar>
  );
}
