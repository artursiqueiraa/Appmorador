import { httpClient } from './httpClient';
import type { DashboardOperacionalResponse } from '../types/api';

export const dashboardService = {
  obterOperacional: () =>
    httpClient.get<DashboardOperacionalResponse>('/api/dashboard-operacional').then((r) => r.data),
};
