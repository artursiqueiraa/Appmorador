import { env } from '../config/env';
import { secureStorage } from '../auth/secureStorage';
import type { EntrarResponse } from './types';

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

async function request<T>(path: string, options: RequestInit = {}, allowRefresh = true): Promise<T> {
  const accessToken = await secureStorage.getAccessToken();

  const response = await fetch(`${env.apiUrl}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...options.headers,
    },
  });

  if (response.status === 401 && allowRefresh && accessToken) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      return request<T>(path, options, false);
    }
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const message = body?.error ?? 'Ocorreu um erro. Tente novamente.';
    throw new ApiError(response.status, message);
  }

  return body as T;
}

/** Uma tentativa só de renovar via refresh token — se falhar, limpa a sessão local. */
async function tryRefresh(): Promise<boolean> {
  const refreshToken = await secureStorage.getRefreshToken();
  if (!refreshToken) {
    return false;
  }

  const response = await fetch(`${env.apiUrl}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  });

  if (!response.ok) {
    await secureStorage.clear();
    return false;
  }

  const data = (await response.json()) as EntrarResponse;
  await secureStorage.setTokens(data.accessToken, data.refreshToken);
  return true;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: 'GET' }),

  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),

  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body !== undefined ? JSON.stringify(body) : undefined }),
};
