import { beforeEach, describe, expect, it } from 'vitest';
import { useAuthStore } from '../authStore';
import { criarFakeJwt } from '../../testUtils/fakeJwt';

describe('authStore', () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.setState({ accessToken: null, refreshToken: null, user: null, impersonation: null, isLoading: false });
  });

  it('setSession persiste token e usuário', () => {
    useAuthStore.getState().setSession('token-abc', 'refresh-abc', { id: '1', nome: 'Carlos', email: 'carlos@teste.com' });

    const estado = useAuthStore.getState();
    expect(estado.accessToken).toBe('token-abc');
    expect(estado.user?.nome).toBe('Carlos');
    expect(localStorage.getItem('painel.accessToken')).toBe('"token-abc"');
  });

  it('clearSession limpa tudo, inclusive impersonation', () => {
    useAuthStore.getState().setSession('token-abc', 'refresh-abc', { id: '1', nome: 'Carlos', email: 'carlos@teste.com' });
    useAuthStore.getState().startImpersonation('token-imp', {
      propriedadeId: 'p1',
      propriedadeNome: 'Casa',
      clienteNome: 'Fernanda',
      tokenOriginal: 'token-abc',
      expiresAtUtc: new Date().toISOString(),
    });

    useAuthStore.getState().clearSession();

    const estado = useAuthStore.getState();
    expect(estado.accessToken).toBeNull();
    expect(estado.impersonation).toBeNull();
    expect(localStorage.getItem('painel.impersonation')).toBeNull();
  });

  it('startImpersonation troca o token ativo para o de impersonation', () => {
    useAuthStore.getState().setSession('token-master', 'refresh-master', { id: '1', nome: 'Master', email: 'm@teste.com' });

    useAuthStore.getState().startImpersonation('token-impersonation', {
      propriedadeId: 'p1',
      propriedadeNome: 'Casa Serra',
      clienteNome: 'Carlos',
      tokenOriginal: 'token-master',
      expiresAtUtc: new Date().toISOString(),
    });

    expect(useAuthStore.getState().accessToken).toBe('token-impersonation');
  });

  it('endImpersonation restaura o token original e retorna o propriedadeId', () => {
    useAuthStore.getState().setSession('token-master', 'refresh-master', { id: '1', nome: 'Master', email: 'm@teste.com' });
    useAuthStore.getState().startImpersonation('token-impersonation', {
      propriedadeId: 'p1',
      propriedadeNome: 'Casa Serra',
      clienteNome: 'Carlos',
      tokenOriginal: 'token-master',
      expiresAtUtc: new Date().toISOString(),
    });

    const propriedadeId = useAuthStore.getState().endImpersonation();

    expect(propriedadeId).toBe('p1');
    expect(useAuthStore.getState().accessToken).toBe('token-master');
    expect(useAuthStore.getState().impersonation).toBeNull();
  });

  it('getDecoded retorna null sem token', () => {
    expect(useAuthStore.getState().getDecoded()).toBeNull();
  });

  it('getDecoded decodifica as claims reais do token', () => {
    const token = criarFakeJwt({ sub: 'user-1', email: 'master@teste.com', role: 'Master', exp: 9999999999 });
    useAuthStore.getState().setSession(token, 'refresh', { id: 'user-1', nome: 'Master', email: 'master@teste.com' });

    const decoded = useAuthStore.getState().getDecoded();

    expect(decoded?.role).toBe('Master');
    expect(decoded?.sub).toBe('user-1');
  });
});
