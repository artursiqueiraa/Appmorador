import { beforeEach, describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { RoleRoute } from '../RoleRoute';
import { useAuthStore } from '../../stores/authStore';
import { criarFakeJwt } from '../../testUtils/fakeJwt';

function logarCom(role?: string) {
  const token = criarFakeJwt({ sub: 'u1', email: 'x@teste.com', ...(role ? { role } : {}), exp: 9999999999 });
  useAuthStore.getState().setSession(token, 'refresh', { id: 'u1', nome: 'X', email: 'x@teste.com' });
}

function renderComRota(caminhoInicial: string) {
  return render(
    <MemoryRouter initialEntries={[caminhoInicial]}>
      <Routes>
        <Route path="/dashboard" element={<div>Dashboard Genérico</div>} />
        <Route element={<RoleRoute permitido={['Master', 'Suporte']} />}>
          <Route path="/clientes" element={<div>Lista de Clientes</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}

describe('RoleRoute', () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.setState({ accessToken: null, refreshToken: null, user: null, impersonation: null, isLoading: false });
  });

  it('papel não permitido: redireciona para /dashboard', () => {
    logarCom('Tecnico');

    renderComRota('/clientes');

    expect(screen.getByText('Dashboard Genérico')).toBeInTheDocument();
  });

  it('papel permitido: renderiza a rota', () => {
    logarCom('Master');

    renderComRota('/clientes');

    expect(screen.getByText('Lista de Clientes')).toBeInTheDocument();
  });

  it('sem nenhum papel global (cliente): redireciona', () => {
    logarCom(undefined);

    renderComRota('/clientes');

    expect(screen.getByText('Dashboard Genérico')).toBeInTheDocument();
  });
});
