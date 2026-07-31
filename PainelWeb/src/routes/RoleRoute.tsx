import { Navigate, Outlet } from 'react-router-dom';
import { usePermissao } from '../hooks/usePermissao';
import type { RoleSistema } from '../types/api';

/** Sprint 22A (Fase 2) — guard de papel: rota inexistente (não só escondida) para quem não tem o RoleGlobal exigido. */
export function RoleRoute({ permitido }: { permitido: RoleSistema[] }) {
  const { temAlgumRole } = usePermissao();

  if (!temAlgumRole(...permitido)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
