import { useEffect } from 'react';
import { BrowserRouter, useNavigate } from 'react-router-dom';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { lightTheme, darkTheme } from './styles/muiTheme';
import { useTemaStore } from './stores/temaStore';
import { useAuthStore } from './stores/authStore';
import { registerSessionExpiredHandler } from './services/httpClient';
import { AppRoutes } from './routes/AppRoutes';
import { GlobalToast } from './components/GlobalToast';
import { useToastStore } from './stores/toastStore';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
});

function SessionExpiredWatcher() {
  const navigate = useNavigate();
  const clearSession = useAuthStore((s) => s.clearSession);
  const mostrarToast = useToastStore((s) => s.mostrar);

  useEffect(() => {
    registerSessionExpiredHandler(() => {
      clearSession();
      mostrarToast('Sua sessão expirou. Faça login novamente.', 'warning');
      navigate('/login', { replace: true });
    });
    return () => registerSessionExpiredHandler(null);
  }, [navigate, clearSession, mostrarToast]);

  return null;
}

function App() {
  const modo = useTemaStore((s) => s.modo);

  return (
    <ThemeProvider theme={modo === 'dark' ? darkTheme : lightTheme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <SessionExpiredWatcher />
          <AppRoutes />
          <GlobalToast />
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

export default App;
