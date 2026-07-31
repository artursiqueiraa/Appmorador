import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { salvarUrlDeRetorno } from '../stores/authStore';

/** Sprint 22A (Fase 2) — sem sessão, redireciona para login salvando a URL para restaurar depois. */
export function PrivateRoute() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    salvarUrlDeRetorno(location.pathname + location.search);
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
