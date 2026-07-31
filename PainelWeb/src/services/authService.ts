import { httpClient } from './httpClient';
import type { EntrarRequest, EntrarResponse, ImpersonarRequest, ImpersonarResponse } from '../types/api';

export const authService = {
  login: (dados: EntrarRequest) => httpClient.post<EntrarResponse>('/api/auth/login', dados).then((r) => r.data),

  logout: (refreshToken: string) => httpClient.post('/api/auth/logout', { refreshToken }),

  impersonar: (dados: ImpersonarRequest) =>
    httpClient.post<ImpersonarResponse>('/api/auth/impersonar', dados).then((r) => r.data),

  encerrarImpersonation: (dados: ImpersonarRequest) => httpClient.post('/api/auth/impersonar/encerrar', dados),
};
