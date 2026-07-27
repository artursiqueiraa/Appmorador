/**
 * Sprint 20 (ADR 0024, Regra 5) — patch parcial da lista de câmeras a partir de um
 * evento SignalR: só a câmera cujo id bate muda, o restante do array preserva a
 * mesma referência de objeto (evita re-render de cards que não mudaram).
 */
import { aplicarAtualizacaoCamera } from '../aplicarAtualizacaoCamera';
import type { CameraResponse } from '../../api/types';
import type { CameraStatusAtualizadaEvento } from '../../realtime/RealtimeContext';

function novaCamera(overrides: Partial<CameraResponse> = {}): CameraResponse {
  return {
    id: 'camera-1',
    nome: 'Entrada',
    status: 'Desconhecido',
    ultimaImagemUrl: null,
    ultimaVezVistaUtc: null,
    ...overrides,
  };
}

function novoEvento(overrides: Partial<CameraStatusAtualizadaEvento> = {}): CameraStatusAtualizadaEvento {
  return {
    propriedadeId: 'propriedade-1',
    cameraId: 'camera-1',
    status: 'Online',
    ultimaImagemUrl: '/api/cameras/camera-1/imagem?v=123',
    ultimaAtualizacaoUtc: '2026-07-26T12:00:00Z',
    recebidoEm: Date.now(),
    ...overrides,
  };
}

describe('aplicarAtualizacaoCamera', () => {
  it('atualiza status/imagem/timestamp só da câmera cujo id bate', () => {
    const cameras = [novaCamera({ id: 'camera-1' }), novaCamera({ id: 'camera-2', nome: 'Sala' })];

    const resultado = aplicarAtualizacaoCamera(cameras, novoEvento());

    expect(resultado[0].status).toBe('Online');
    expect(resultado[0].ultimaImagemUrl).toBe('/api/cameras/camera-1/imagem?v=123');
    expect(resultado[0].ultimaVezVistaUtc).toBe('2026-07-26T12:00:00Z');
  });

  it('nunca altera as demais câmeras da lista', () => {
    const cameraSala = novaCamera({ id: 'camera-2', nome: 'Sala', status: 'Offline' });
    const cameras = [novaCamera({ id: 'camera-1' }), cameraSala];

    const resultado = aplicarAtualizacaoCamera(cameras, novoEvento());

    expect(resultado[1]).toBe(cameraSala);
  });

  it('evento sem ultimaImagemUrl preserva a imagem anterior da câmera', () => {
    const cameras = [novaCamera({ id: 'camera-1', ultimaImagemUrl: '/api/cameras/camera-1/imagem?v=antiga' })];

    const resultado = aplicarAtualizacaoCamera(cameras, novoEvento({ ultimaImagemUrl: null }));

    expect(resultado[0].ultimaImagemUrl).toBe('/api/cameras/camera-1/imagem?v=antiga');
  });

  it('evento de uma câmera que não está na lista não quebra nem altera nada', () => {
    const cameras = [novaCamera({ id: 'camera-1' })];

    const resultado = aplicarAtualizacaoCamera(cameras, novoEvento({ cameraId: 'camera-inexistente' }));

    expect(resultado).toEqual(cameras);
  });
});
