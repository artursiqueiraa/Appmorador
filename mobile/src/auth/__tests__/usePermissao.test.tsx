/**
 * Sprint 21 (ADR 0021/0025/0026) — usePermissao é a única fonte de verdade no
 * app para "o que este usuário pode fazer"/"o que esta propriedade contratou".
 * Cobre o caso fail-closed (sem propriedade selecionada, tudo negado) e a leitura
 * correta de permissoes/features quando uma propriedade está selecionada — nunca
 * confiando no `perfil` local de `profilePreference.ts` (preferência de UI, sem
 * relação com este modelo).
 */
import React from 'react';
import { renderHook, act, waitFor } from '@testing-library/react-native';
import { AuthProvider, useAuth } from '../AuthContext';
import { usePermissao } from '../usePermissao';
import type { PropriedadeResponse } from '../../api/types';

jest.mock('../secureStorage', () => ({
  secureStorage: {
    getAccessToken: jest.fn().mockResolvedValue(null),
    getRefreshToken: jest.fn().mockResolvedValue(null),
    getUser: jest.fn().mockResolvedValue(null),
    setTokens: jest.fn().mockResolvedValue(undefined),
    setUser: jest.fn().mockResolvedValue(undefined),
    clear: jest.fn().mockResolvedValue(undefined),
  },
}));

jest.mock('../profilePreference', () => ({
  obterPerfil: jest.fn().mockResolvedValue('morador'),
  salvarPerfil: jest.fn(),
}));

jest.mock('../../api/client', () => ({
  api: { post: jest.fn() },
  registerSessionExpiredHandler: jest.fn(),
}));

function wrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

function novaPropriedade(overrides: Partial<PropriedadeResponse> = {}): PropriedadeResponse {
  return {
    id: 'prop-1',
    nome: 'Casa Serra',
    tipo: 'Residencial',
    perfil: 'Administrador',
    permissoes: ['VerCameras', 'AbrirPortao'],
    features: ['Cameras'],
    ...overrides,
  };
}

function useHooks() {
  const auth = useAuth();
  const permissao = usePermissao();
  return { auth, permissao };
}

describe('usePermissao', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it('sem propriedade selecionada: perfil é null', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    expect(result.current.permissao.perfil).toBeNull();
  });

  it('sem propriedade selecionada: permissoes é uma lista vazia', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    expect(result.current.permissao.permissoes).toEqual([]);
  });

  it('sem propriedade selecionada: features é uma lista vazia', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    expect(result.current.permissao.features).toEqual([]);
  });

  it('sem propriedade selecionada: temPermissao nega qualquer permissão (fail-closed)', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    expect(result.current.permissao.temPermissao('AbrirPortao')).toBe(false);
  });

  it('sem propriedade selecionada: temFeature nega qualquer feature (fail-closed)', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    expect(result.current.permissao.temFeature('Cameras')).toBe(false);
  });

  it('com propriedade selecionada: perfil reflete o da propriedade', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ perfil: 'Administrador' })));

    expect(result.current.permissao.perfil).toBe('Administrador');
  });

  it('com propriedade selecionada: temPermissao retorna true para permissão concedida', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ permissoes: ['CadastrarMorador'] })));

    expect(result.current.permissao.temPermissao('CadastrarMorador')).toBe(true);
  });

  it('com propriedade selecionada: temPermissao retorna false para permissão não concedida', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ permissoes: ['CadastrarMorador'] })));

    expect(result.current.permissao.temPermissao('ConfigurarPgm')).toBe(false);
  });

  it('com propriedade selecionada: temFeature retorna true para feature ativa', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ features: ['Cameras', 'Push'] })));

    expect(result.current.permissao.temFeature('Cameras')).toBe(true);
  });

  it('com propriedade selecionada: temFeature retorna false para feature não contratada', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ features: [] })));

    expect(result.current.permissao.temFeature('Cameras')).toBe(false);
  });

  it('trocar de propriedade atualiza permissoes/features imediatamente (nunca mistura dado da propriedade anterior)', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ id: 'prop-1', permissoes: ['VerCameras'], features: ['Cameras'] })));
    expect(result.current.permissao.temPermissao('VerCameras')).toBe(true);
    expect(result.current.permissao.temFeature('Cameras')).toBe(true);

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ id: 'prop-2', permissoes: [], features: [] })));
    expect(result.current.permissao.temPermissao('VerCameras')).toBe(false);
    expect(result.current.permissao.temFeature('Cameras')).toBe(false);
  });

  it('com múltiplas permissoes concedidas: temPermissao reconhece cada uma independentemente', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () =>
      result.current.auth.selectProperty(
        novaPropriedade({ permissoes: ['CadastrarMorador', 'VerCameras', 'AbrirPortao'] }),
      ),
    );

    expect(result.current.permissao.temPermissao('CadastrarMorador')).toBe(true);
    expect(result.current.permissao.temPermissao('VerCameras')).toBe(true);
    expect(result.current.permissao.temPermissao('AbrirPortao')).toBe(true);
    expect(result.current.permissao.temPermissao('ConfigurarPgm')).toBe(false);
  });

  it('com múltiplas features ativas: temFeature reconhece cada uma independentemente', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ features: ['Cameras', 'Push', 'Snapshot'] })));

    expect(result.current.permissao.temFeature('Cameras')).toBe(true);
    expect(result.current.permissao.temFeature('Push')).toBe(true);
    expect(result.current.permissao.temFeature('Snapshot')).toBe(true);
    expect(result.current.permissao.temFeature('Ia')).toBe(false);
  });

  it('expõe a lista de permissoes tal como veio da propriedade selecionada', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ permissoes: ['CriarVisitante'] })));

    expect(result.current.permissao.permissoes).toEqual(['CriarVisitante']);
  });

  it('expõe a lista de features tal como veio da propriedade selecionada', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ features: ['InterfoneSip'] })));

    expect(result.current.permissao.features).toEqual(['InterfoneSip']);
  });

  it('perfil Morador (ainda inalcançável via login real, mas o hook deve refletir o valor sem lançar)', async () => {
    const { result } = await renderHook(() => useHooks(), { wrapper });
    await waitFor(() => expect(result.current.auth.isLoading).toBe(false));

    await act(async () => result.current.auth.selectProperty(novaPropriedade({ perfil: 'Morador' })));

    expect(result.current.permissao.perfil).toBe('Morador');
  });
});
