import { Platform } from 'react-native';
import * as Notifications from 'expo-notifications';
import Constants from 'expo-constants';
import { api } from '../api/client';
import type {
  AtualizarPreferenciasDispositivoPushRequest,
  DispositivoPushResponse,
  RegistrarDispositivoPushRequest,
} from '../api/types';
import { obterDispositivoPushId, salvarDispositivoPushId, limparDispositivoPushId, salvarPreferenciasLocais } from './pushDeviceStorage';

export type StatusPermissaoPush = 'concedida' | 'negada' | 'nao-solicitada';

async function statusAtualAsync(): Promise<StatusPermissaoPush> {
  const { status } = await Notifications.getPermissionsAsync();
  if (status === 'granted') {
    return 'concedida';
  }
  if (status === 'denied') {
    return 'negada';
  }
  return 'nao-solicitada';
}

export const obterStatusPermissaoAsync = statusAtualAsync;

/**
 * Sprint 19 — sem Firebase configurado no build (sem `google-services.json`,
 * dívida técnica documentada na ADR 0023), `getDevicePushTokenAsync` pode falhar
 * ou devolver um token que não corresponde a nenhum projeto Firebase real. Em
 * qualquer um dos dois casos, o registro nunca pode travar login/uso do app —
 * falha aqui é sempre silenciosa (o app segue funcionando normalmente, só sem
 * push, o mesmo modo "sem-op documentado" do backend).
 */
async function registrarTokenAtualAsync(propriedadeId?: string | null): Promise<DispositivoPushResponse | null> {
  try {
    const devicePushToken = await Notifications.getDevicePushTokenAsync();
    const request: RegistrarDispositivoPushRequest = {
      propriedadeId: propriedadeId ?? null,
      plataforma: Platform.OS === 'ios' ? 'Ios' : 'Android',
      token: String(devicePushToken.data),
      modelo: `${Platform.OS} ${Platform.Version ?? ''}`.trim(),
      versaoApp: Constants.expoConfig?.version ?? null,
    };
    const response = await api.post<DispositivoPushResponse>('/api/dispositivos-push', request);
    await salvarDispositivoPushId(response.id);
    return response;
  } catch {
    return null;
  }
}

/**
 * Sprint 19 (Fase 7.1) — só mostra o diálogo nativo de permissão se ainda nunca
 * foi decidido; se o morador já negou antes, nunca insiste (o Android/iOS também
 * não deixaria reabrir o diálogo nesse caso — só `Linking.openSettings()`
 * reativa, ver `NotificacoesScreen.tsx`).
 */
export async function solicitarPermissaoERegistrarAsync(propriedadeId?: string | null): Promise<StatusPermissaoPush> {
  const atual = await statusAtualAsync();
  let status = atual;

  if (atual === 'nao-solicitada') {
    const resultado = await Notifications.requestPermissionsAsync();
    status = resultado.granted ? 'concedida' : 'negada';
  }

  if (status === 'concedida') {
    await registrarTokenAtualAsync(propriedadeId);
  }

  return status;
}

/** Reenvia o registro (ex.: propriedade selecionada mudou) sem exibir nenhum diálogo — só se a permissão já tiver sido concedida antes. */
export async function reenviarRegistroSeJaPermitidoAsync(propriedadeId?: string | null): Promise<void> {
  const status = await statusAtualAsync();
  if (status === 'concedida') {
    await registrarTokenAtualAsync(propriedadeId);
  }
}

/** Sprint 19 (Fase 6.2) — chamado pelo listener de refresh de token (`PushNotificationProvider`). */
export async function atualizarTokenAsync(novoToken: string): Promise<void> {
  const id = await obterDispositivoPushId();
  if (!id) {
    return;
  }
  try {
    await api.put(`/api/dispositivos-push/${id}`, { token: novoToken });
  } catch {
    // best-effort — na próxima abertura do app, solicitarPermissaoERegistrarAsync tenta de novo.
  }
}

export async function atualizarPreferenciasAsync(
  preferencias: AtualizarPreferenciasDispositivoPushRequest,
): Promise<DispositivoPushResponse | null> {
  const id = await obterDispositivoPushId();
  if (!id) {
    return null;
  }
  const response = await api.put<DispositivoPushResponse>(`/api/dispositivos-push/${id}/preferencias`, preferencias);
  await salvarPreferenciasLocais(preferencias);
  return response;
}

/** Sprint 19 (Fase 6.3) — chamado no logout, antes da sessão ser limpa (ver `AuthContext.registerBeforeLogoutHook`). */
export async function desregistrarAsync(): Promise<void> {
  const id = await obterDispositivoPushId();
  if (!id) {
    return;
  }
  try {
    await api.delete(`/api/dispositivos-push/${id}`);
  } catch {
    // best-effort — DesativarAsync no backend é idempotente; sem rede agora, o
    // dispositivo continua ativo no servidor até expirar ou ser substituído.
  } finally {
    await limparDispositivoPushId();
  }
}
