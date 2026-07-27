/**
 * Sprint 20 (ADR 0024) — a Api serve imagem de câmera autenticada (Bearer), nunca
 * via static files públicos; este hook é quem monta o header que `expo-image`
 * anexa na requisição. Sem token ainda carregado, o header fica `undefined`
 * (nunca uma string quebrada tipo "Bearer undefined").
 */
import { renderHook, waitFor } from '@testing-library/react-native';
import { useAuthHeader } from '../useAuthHeader';
import { secureStorage } from '../../auth/secureStorage';

jest.mock('../../auth/secureStorage', () => ({
  secureStorage: {
    getAccessToken: jest.fn(),
  },
}));

describe('useAuthHeader', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it('sem token, o header permanece undefined', async () => {
    (secureStorage.getAccessToken as jest.Mock).mockResolvedValue(null);

    const { result } = await renderHook(() => useAuthHeader());

    await waitFor(() => expect(secureStorage.getAccessToken).toHaveBeenCalled());
    expect(result.current).toBeUndefined();
  });

  it('com token, monta o header Authorization Bearer', async () => {
    (secureStorage.getAccessToken as jest.Mock).mockResolvedValue('token-abc');

    const { result } = await renderHook(() => useAuthHeader());

    await waitFor(() => expect(result.current).toEqual({ Authorization: 'Bearer token-abc' }));
  });
});
