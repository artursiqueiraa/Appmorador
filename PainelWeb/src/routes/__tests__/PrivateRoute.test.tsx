import { beforeEach, describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { PrivateRoute } from '../PrivateRoute';
import { useAuthStore } from '../../stores/authStore';

function renderComRota(caminhoInicial: string) {
  return render(
    <MemoryRouter initialEntries={[caminhoInicial]}>
      <Routes>
        <Route path="/login" element={<div>Tela de Login</div>} />
        <Route element={<PrivateRoute />}>
          <Route path="/dashboard" element={<div>Tela Protegida</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}

describe('PrivateRoute', () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.setState({ accessToken: null, refreshToken: null, user: null, impersonation: null, isLoading: false });
  });

  it('sem sessão: redireciona para /login', () => {
    renderComRota('/dashboard');

    expect(screen.getByText('Tela de Login')).toBeInTheDocument();
  });

  it('com sessão: renderiza a rota protegida', () => {
    useAuthStore.getState().setSession('token', 'refresh', { id: '1', nome: 'Carlos', email: 'c@teste.com' });

    renderComRota('/dashboard');

    expect(screen.getByText('Tela Protegida')).toBeInTheDocument();
  });
});
