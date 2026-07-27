import type { CameraResponse } from '../api/types';
import type { CameraStatusAtualizadaEvento } from '../realtime/RealtimeContext';

/**
 * Sprint 20 (ADR 0024, Regra 5) — patch parcial de uma lista de câmeras a partir de
 * um evento SignalR: só a câmera cujo id bate é substituída, o resto do array
 * mantém a mesma referência de objeto (evita re-render de cards que não mudaram).
 * Extraído da `CamerasScreen` para ser testável sem precisar renderizar a tela
 * inteira (navegação/auth/api mockados).
 */
export function aplicarAtualizacaoCamera(cameras: CameraResponse[], evento: CameraStatusAtualizadaEvento): CameraResponse[] {
  return cameras.map((camera) =>
    camera.id === evento.cameraId
      ? {
          ...camera,
          status: evento.status,
          ultimaImagemUrl: evento.ultimaImagemUrl ?? camera.ultimaImagemUrl,
          ultimaVezVistaUtc: evento.ultimaAtualizacaoUtc ?? camera.ultimaVezVistaUtc,
        }
      : camera,
  );
}
