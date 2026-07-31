import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Alert, Box, Button, TextField } from '@mui/material';
import { useAuth } from '../hooks/useAuth';
import { extrairMensagemErro } from '../services/httpClient';
import { consumirUrlDeRetorno } from '../stores/authStore';

/** Sprint 22A (Fase 2) — validação em tempo real (HTML5 required/email) + erro amigável (nunca a mensagem técnica crua). */
export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState<string | null>(null);
  const [carregando, setCarregando] = useState(false);

  const handleSubmit = async (evento: FormEvent) => {
    evento.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      await login(email, senha);
      const urlRetorno = consumirUrlDeRetorno();
      navigate(urlRetorno ?? '/dashboard', { replace: true });
    } catch (err) {
      setErro(extrairMensagemErro(err, 'E-mail ou senha inválidos.'));
    } finally {
      setCarregando(false);
    }
  };

  return (
    <Box
      component="form"
      onSubmit={(e) => void handleSubmit(e)}
      sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}
    >
      {erro ? <Alert severity="error">{erro}</Alert> : null}
      <TextField
        label="E-mail"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
        autoFocus
        fullWidth
      />
      <TextField
        label="Senha"
        type="password"
        value={senha}
        onChange={(e) => setSenha(e.target.value)}
        required
        fullWidth
      />
      <Button type="submit" variant="contained" size="large" loading={carregando} fullWidth>
        Entrar
      </Button>
    </Box>
  );
}
