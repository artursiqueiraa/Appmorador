import { Navigate, Route, Routes } from 'react-router-dom';
import { AuthLayout } from '../layouts/AuthLayout';
import { MainLayout } from '../layouts/MainLayout';
import { LoginPage } from '../pages/LoginPage';
import { DashboardOperacionalPage } from '../pages/DashboardOperacionalPage';
import { DashboardTecnicoPage } from '../pages/DashboardTecnicoPage';
import { ClientesListPage } from '../pages/ClientesListPage';
import { ClienteDetalhePage } from '../pages/ClienteDetalhePage';
import { SuporteDiagnosticoPage } from '../pages/SuporteDiagnosticoPage';
import { SuporteSessoesAtivasPage } from '../pages/SuporteSessoesAtivasPage';
import { SuporteLogsPage } from '../pages/SuporteLogsPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { PrivateRoute } from './PrivateRoute';
import { RoleRoute } from './RoleRoute';
import { EquipamentosListPage } from '../modulos/equipamentos/EquipamentosListPage';
import { ProvisionamentosPage } from '../modulos/provisionamentos/ProvisionamentosPage';
import { DiagnosticoEquipamentosPage } from '../modulos/diagnostico/DiagnosticoEquipamentosPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AuthLayout />}>
        <Route path="/login" element={<LoginPage />} />
      </Route>

      <Route element={<PrivateRoute />}>
        <Route element={<MainLayout />}>
          <Route path="/dashboard" element={<DashboardOperacionalPage />} />

          <Route element={<RoleRoute permitido={['Tecnico']} />}>
            <Route path="/dashboard-tecnico" element={<DashboardTecnicoPage />} />
          </Route>

          <Route element={<RoleRoute permitido={['Master', 'Suporte']} />}>
            <Route path="/clientes" element={<ClientesListPage />} />
            <Route path="/clientes/:id" element={<ClienteDetalhePage />} />
            <Route path="/suporte/selecionar-cliente" element={<ClientesListPage />} />
            <Route path="/suporte/diagnostico" element={<SuporteDiagnosticoPage />} />
            <Route path="/suporte/sessoes-ativas" element={<SuporteSessoesAtivasPage />} />
            <Route path="/suporte/logs" element={<SuporteLogsPage />} />
          </Route>

          <Route element={<RoleRoute permitido={['Master', 'Tecnico']} />}>
            <Route path="/equipamentos" element={<EquipamentosListPage />} />
            <Route path="/provisionamentos" element={<ProvisionamentosPage />} />
            <Route path="/diagnostico-equipamentos" element={<DiagnosticoEquipamentosPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
