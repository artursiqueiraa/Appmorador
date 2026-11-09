import { Platform } from 'react-native';
import * as Notifications from 'expo-notifications';

/**
 * Sprint 19 (ADR 0023, Fase 9) — os 3 canais Android da missão. Depois de criado,
 * um canal só pode ter nome/descrição alterados pelo próprio Android (limitação do
 * SO) — os valores de importância/som/vibração aqui são o registro definitivo na
 * primeira vez que o app roda em cada aparelho.
 */
/** Sprint 24 — mesmo id usado pelo backend (FirebaseNotificationProvider.CanalCritico). */
export const CANAL_CRITICO = 'alertas_criticos';

export async function configurarCanaisAndroidAsync(): Promise<void> {
  if (Platform.OS !== 'android') {
    return;
  }

  // Sprint 24 (ADR 0040) — alertas críticos (disparo, pânico, violação, central sem comunicação):
  // importância máxima, som de alarme próprio (assets/sons, registrado no plugin do app.json),
  // vibração longa e pedido para tocar mesmo no "Não perturbe" (o Android só obedece se o usuário
  // permitir nos ajustes do app — a tela "Alertas e emergência" leva até lá). O id e o nome do
  // som precisam bater com FirebaseNotificationProvider.CanalCritico/SomCritico no backend.
  await Notifications.setNotificationChannelAsync(CANAL_CRITICO, {
    name: 'Alertas críticos',
    description: 'Alarme disparado, pânico, violação e central sem comunicação.',
    importance: Notifications.AndroidImportance.MAX,
    sound: 'alerta_critico.wav',
    enableVibrate: true,
    vibrationPattern: [0, 800, 400, 800, 400, 800],
    bypassDnd: true,
    lockscreenVisibility: Notifications.AndroidNotificationVisibility.PUBLIC,
    audioAttributes: { usage: Notifications.AndroidAudioUsage.ALARM, contentType: Notifications.AndroidAudioContentType.SONIFICATION },
  });

  await Notifications.setNotificationChannelAsync('alertas', {
    name: 'Alarmes e alertas',
    importance: Notifications.AndroidImportance.HIGH,
    enableVibrate: true,
    vibrationPattern: [0, 250, 250, 250],
  });

  await Notifications.setNotificationChannelAsync('atividades', {
    name: 'Atividades em casa',
    importance: Notifications.AndroidImportance.DEFAULT,
    enableVibrate: true,
    vibrationPattern: [0, 250],
  });

  await Notifications.setNotificationChannelAsync('geral', {
    name: 'Mudanças de status',
    importance: Notifications.AndroidImportance.LOW,
    enableVibrate: false,
  });
}
