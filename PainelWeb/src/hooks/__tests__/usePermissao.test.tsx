import { beforeEach, describe, expect, it } from 'vitest';
import { renderHook } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { usePermissao } from '../usePermissao';
import { useAuthStore } from '../../stores/authStore';
import { criarFakeJwt } from '../../testUtils/fakeJwt';
import type { ReactNode } from 'react';

function wrapper({ children }: { children: ReactNode }) {
  return <MemoryRouter>{children}</MemoryRouter>;
}

function logarCom(role?: string) {
  const token = criarFakeJwt({ sub: 'u1', email: 'x@teste.com', ...(role ? { role } : {}), exp: 9999999999 });
  useAuthStore.getState().setSession(token, 'refresh', { id: 'u1', nome: 'X', email: 'x@teste.com' });
}

describe('usePermissao', () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.setState({ accessToken: null, refreshToken: null, user: null, impersonation: null, isLoading: false });
  });

  it('sem sessão: nenhum papel é reconhecido', () => {
    const { result } = renderHook(() => usePermissao(), { wrapper });

    expect(result.current.roleGlobal).toBeNull();
    expect(result.current.isMaster).toBe(false);
    expect(result.current.podeImpersonar).toBe(false);
  });

  it('Master: isMaster e podeImpersonar verdadeiros', () => {
    logarCom('Master');
    const { result } = renderHook(() => usePermissao(), { wrapper });

    expect(result.current.isMaster).toBe(true);
    expect(result.current.podeImpersonar).toBe(true);
    expect(result.current.podeVerTudo).toBe(true);
  });

  it('Suporte: podeImpersonar verdadeiro, isMaster falso', () => {
    logarCom('Suporte');
    const { result } = renderHook(() => usePermissao(), { wrapper });

    expect(result.current.isSuporte).toBe(true);
    expect(result.current.isMaster).toBe(false);
    expect(result.current.podeImpersonar).toBe(true);
  });

  it('Tecnico: não pode impersonar nem ver tudo', () => {
    logarCom('Tecnico');
    const { result } = renderHook(() => usePermissao(), { wrapper });

    expect(result.current.isTecnico).toBe(true);
    expect(result.current.podeImpersonar).toBe(false);
    expect(result.current.podeVerTudo).toBe(false);
  });

  it('temAlgumRole reconhece o papel atual dentro de uma lista', () => {
    logarCom('Tecnico');
    const { result } = renderHook(() => usePermissao(), { wrapper });

    expect(result.current.temAlgumRole('Master', 'Tecnico')).toBe(true);
    expect(result.current.temAlgumRole('Master', 'Suporte')).toBe(false);
  });

  it('cliente autenticado (sem claim role): nenhum papel interno reconhecido', () => {
    logarCom(undefined);
    const { result } = renderHook(() => usePermissao(), { wrapper });

    expect(result.current.roleGlobal).toBeNull();
    expect(result.current.isMaster).toBe(false);
    expect(result.current.isTecnico).toBe(false);
    expect(result.current.isSuporte).toBe(false);
  });
});
