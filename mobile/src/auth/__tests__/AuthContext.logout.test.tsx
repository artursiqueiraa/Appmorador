/**
 * Sprint 18.1 (hotfix) — cobre a causa raiz confirmada de "não sai da conta":
 * antes desta Sprint, se a revogação do refresh token no servidor travasse ou
 * lançasse fora do bloco protegido, `setUser(null)` nunca era chamado e o
 * morador ficava preso na sessão anterior. Agora a limpeza local roda sempre,
 * dentro de um `finally`, independente do que acontece com a chamada de rede.
 */
import React from 'react';
import { renderHook, act, waitFor } from '@testing-library/react-native';
import { AuthProvider, useAuth } from '../AuthContext';
import { secureStorage } from '../secureStorage';
import { api } from '../../api/client';

jest.mock('../secureStorage', () => ({
  secureStorage: {
    getAccessToken: jest.fn().mockResolvedValue('token-existente'),
    getRefreshToken: jest.fn().mockResolvedValue('refresh-existente'),
    getUser: jest.fn().mockResolvedValue({ id: '1', nome: 'Morador Teste', email: 'morador@teste.com' }),
    clear: jest.fn().mockResolvedValue(undefined),
  },
}));

jest.mock('../profilePreference', () => ({
  obterPerfil: jest.fn().mockResolvedValue('morador'),
  salvarPerfil: jest.fn(),
}));

jest.mock('../../api/client', () => ({
  api: {
    post: jest.fn(),
  },
  registerSessionExpiredHandler: jest.fn(),
}));

function wrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

describe('AuthContext.logout — limpeza local garantida mesmo com falha de rede', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it('limpa o usuário mesmo quando a revogação no servidor rejeita', async () => {
    (api.post as jest.Mock).mockRejectedValue(new Error('tempo esgotado'));

    const { result } = await renderHook(() => useAuth(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    await waitFor(() => expect(result.current.user).not.toBeNull());

    await act(async () => {
      await result.current.logout();
    });

    expect(result.current.user).toBeNull();
    expect(secureStorage.clear).toHaveBeenCalled();
  });

  it('limpa o usuário mesmo quando secureStorage.clear() falha', async () => {
    (api.post as jest.Mock).mockResolvedValue(undefined);
    (secureStorage.clear as jest.Mock).mockRejectedValueOnce(new Error('falha ao limpar'));

    const { result } = await renderHook(() => useAuth(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    await waitFor(() => expect(result.current.user).not.toBeNull());

    await act(async () => {
      await result.current.logout();
    });

    expect(result.current.user).toBeNull();
  });
});
