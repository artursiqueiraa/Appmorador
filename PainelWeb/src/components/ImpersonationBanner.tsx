import { useEffect, useState } from 'react';
import { Box, Button, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';
import { authService } from '../services/authService';
import { useToastStore } from '../stores/toastStore';

function formatarTempoRestante(msRestante: number): string {
  const segundosTotais = Math.max(0, Math.floor(msRestante / 1000));
  const minutos = Math.floor(segundosTotais / 60);
  const segundos = segundosTotais % 60;
  return `${minutos}:${segundos.toString().padStart(2, '0')}`;
}

/**
 * Sprint 22A (Fase 6) — banner LARANJA, fixo no topo, em TODAS as telas durante impersonation.
 * Timer regressivo calculado a partir de `expiresAtUtc` (token de 15min, sem refresh — ver ADR
 * 0021) — quando chega a zero, o token já expirou sozinho no backend; o banner só reflete isso.
 */
export function ImpersonationBanner() {
  const navigate = useNavigate();
  const { impersonation, endImpersonation } = useAuthStore();
  const [msRestante, setMsRestante] = useState(0);
  const [encerrando, setEncerrando] = useState(false);
  const mostrarToast = useToastStore((s) => s.mostrar);

  useEffect(() => {
    if (!impersonation) return;

    const atualizar = () => setMsRestante(new Date(impersonation.expiresAtUtc).getTime() - Date.now());
    atualizar();
    const intervalo = setInterval(atualizar, 1000);
    return () => clearInterval(intervalo);
  }, [impersonation]);

  if (!impersonation) return null;

  const encerrar = async () => {
    setEncerrando(true);
    try {
      await authService.encerrarImpersonation({ propriedadeId: impersonation.propriedadeId }).catch(() => {
        // A limpeza local acontece de qualquer forma — o token expiraria sozinho em 15min mesmo se isto falhar.
      });
    } finally {
      endImpersonation();
      setEncerrando(false);
      mostrarToast('Sessão de impersonation encerrada.', 'info');
      navigate('/suporte/selecionar-cliente', { replace: true });
    }
  };

  return (
    <Box
      sx={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        zIndex: (theme) => theme.zIndex.drawer + 10,
        bgcolor: 'warning.main',
        color: '#1A1200',
        px: 2,
        py: 1,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
      }}
    >
      <Typography variant="body1" sx={{ fontWeight: 600 }}>
        Você está atuando como {impersonation.clienteNome} — {impersonation.propriedadeNome} — expira em{' '}
        {formatarTempoRestante(msRestante)}
      </Typography>
      <Button size="small" variant="contained" color="inherit" onClick={() => void encerrar()} disabled={encerrando}>
        Encerrar Sessão do Cliente
      </Button>
    </Box>
  );
}
