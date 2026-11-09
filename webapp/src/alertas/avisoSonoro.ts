import { Vibration } from 'react-native';
import * as Notifications from 'expo-notifications';
import type { AlertaCriticoResponse } from '../api/types';
import { CANAL_CRITICO } from '../notifications/pushChannels';

const PADRAO_VIBRACAO = [0, 800, 400, 800, 400];

const idNotificacao = (alertaId: string) => `alerta-${alertaId}`;

/**
 * Sprint 24 (ADR 0040) — som e vibração enquanto a tela de alerta crítico está aberta (celular).
 * O som vem de uma notificação local no canal "Alertas críticos" (o mesmo som de alarme do push,
 * importância máxima), sem biblioteca de áudio nova. A vibração repete até alguém responder.
 * `local: true` evita que o handler de notificação tente reabrir o alerta que já está na tela.
 */
export function iniciarAvisoAlerta(alerta: AlertaCriticoResponse): void {
  Vibration.vibrate(PADRAO_VIBRACAO, true);
  Notifications.scheduleNotificationAsync({
    identifier: idNotificacao(alerta.id),
    content: {
      title: `🚨 ${alerta.titulo}`,
      body: alerta.descricao,
      sound: 'alerta_critico.wav',
      priority: Notifications.AndroidNotificationPriority.MAX,
      data: { acao: 'ABRIR_ALERTA', alertaId: alerta.id, local: true },
    },
    trigger: { channelId: CANAL_CRITICO },
  }).catch(() => {
    // Sem permissão de notificação: fica só a vibração e a tela cheia.
  });
}

export function pararAvisoAlerta(alertaId: string): void {
  Vibration.cancel();
  Notifications.dismissNotificationAsync(idNotificacao(alertaId)).catch(() => {});
}
