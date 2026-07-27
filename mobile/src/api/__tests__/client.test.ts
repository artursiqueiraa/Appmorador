/**
 * Sprint 18.1 (hotfix) — cobre a causa raiz confirmada de dois bugs críticos
 * ("propriedades não carregam" e "não sai da conta"): antes desta Sprint,
 * `fetch` não tinha nenhum timeout — um backend inalcançável deixava a
 * requisição pendurada para sempre. Este teste garante que isso nunca mais
 * volta a acontecer sem ser percebido.
 */
import { api, ApiError } from '../client';

jest.mock('../../auth/secureStorage', () => ({
  secureStorage: {
    getAccessToken: jest.fn().mockResolvedValue(null),
  },
}));

jest.mock('@react-native-community/netinfo', () => ({
  fetch: jest.fn().mockResolvedValue({ isConnected: true }),
}));

jest.mock('../../config/env', () => ({
  env: { apiUrl: 'http://localhost:5027' },
}));

describe('api/client — timeout de requisição', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  it('aborta e lança um ApiError amigável quando o backend nunca responde (15s)', async () => {
    // fetch que nunca resolve nem rejeita por conta própria — só o AbortController deve encerrá-lo.
    let capturedSignal: AbortSignal | undefined;
    globalThis.fetch = jest.fn((_url: unknown, options?: RequestInit) => {
      capturedSignal = options?.signal ?? undefined;
      return new Promise((_resolve, reject) => {
        capturedSignal?.addEventListener('abort', () => {
          const err = new Error('Aborted');
          err.name = 'AbortError';
          reject(err);
        });
      });
    }) as unknown as typeof fetch;

    const promessa = api.get('/api/properties');
    // Deixa o assert observar a rejeição antes de avançar os timers.
    const expectativa = expect(promessa).rejects.toBeInstanceOf(ApiError);

    await jest.advanceTimersByTimeAsync(15000);

    await expectativa;
  });

  it('mensagem do timeout é amigável, nunca menciona "AbortError" ou termos técnicos', async () => {
    let capturedSignal: AbortSignal | undefined;
    globalThis.fetch = jest.fn((_url: unknown, options?: RequestInit) => {
      capturedSignal = options?.signal ?? undefined;
      return new Promise((_resolve, reject) => {
        capturedSignal?.addEventListener('abort', () => {
          const err = new Error('Aborted');
          err.name = 'AbortError';
          reject(err);
        });
      });
    }) as unknown as typeof fetch;

    const promessa = api.get('/api/properties');
    const expectativa = promessa.catch((err: unknown) => err);

    await jest.advanceTimersByTimeAsync(15000);

    const erro = (await expectativa) as ApiError;
    expect(erro).toBeInstanceOf(ApiError);
    expect(erro.message.toLowerCase()).not.toContain('abort');
    expect(erro.message.toLowerCase()).not.toContain('exception');
  });
});
