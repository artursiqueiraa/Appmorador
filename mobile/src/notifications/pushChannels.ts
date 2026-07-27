import { Platform } from 'react-native';
import * as Notifications from 'expo-notifications';

/**
 * Sprint 19 (ADR 0023, Fase 9) — os 3 canais Android da missão. Depois de criado,
 * um canal só pode ter nome/descrição alterados pelo próprio Android (limitação do
 * SO) — os valores de importância/som/vibração aqui são o registro definitivo na
 * primeira vez que o app roda em cada aparelho.
 */
export async function configurarCanaisAndroidAsync(): Promise<void> {
  if (Platform.OS !== 'android') {
    return;
  }

  await Notifications.setNotificationChannelAsync('alertas', {
    name: 'Alarmes e alertas',
    importance: Notifications.AndroidImportance.HIGH,
    sound: 'default',
    enableVibrate: true,
    vibrationPattern: [0, 250, 250, 250],
  });

  await Notifications.setNotificationChannelAsync('atividades', {
    name: 'Atividades em casa',
    importance: Notifications.AndroidImportance.DEFAULT,
    sound: 'default',
    enableVibrate: true,
    vibrationPattern: [0, 250],
  });

  await Notifications.setNotificationChannelAsync('geral', {
    name: 'Mudanças de status',
    importance: Notifications.AndroidImportance.LOW,
    sound: 'default',
    enableVibrate: false,
  });
}
