/**
 * Web App — no navegador push é explicitamente indisponível: nunca pede
 * permissão, nunca registra token no backend (que só conhece Ios/Android).
 */
import { api } from '../../api/client';
import * as Notifications from 'expo-notifications';

jest.mock('react-native', () => ({ Platform: { OS: 'web', Version: undefined } }));

jest.mock('../../api/client', () => ({
  api: { post: jest.fn(), put: jest.fn(), delete: jest.fn() },
}));

jest.mock('expo-notifications', () => ({
  getPermissionsAsync: jest.fn(),
  requestPermissionsAsync: jest.fn(),
  getDevicePushTokenAsync: jest.fn(),
}));

jest.mock('expo-constants', () => ({ expoConfig: { version: '1.0.0' } }));

jest.mock('../pushDeviceStorage', () => ({
  obterDispositivoPushId: jest.fn().mockResolvedValue('id-qualquer'),
  salvarDispositivoPushId: jest.fn(),
  limparDispositivoPushId: jest.fn(),
  salvarPreferenciasLocais: jest.fn(),
}));

// eslint-disable-next-line import/first
import {
  atualizarTokenAsync,
  obterStatusPermissaoAsync,
  reenviarRegistroSeJaPermitidoAsync,
  solicitarPermissaoERegistrarAsync,
} from '../pushService';

describe('pushService (web)', () => {
  it('informa indisponível sem consultar a permissão nativa', async () => {
    await expect(obterStatusPermissaoAsync()).resolves.toBe('indisponivel');
    expect(Notifications.getPermissionsAsync).not.toHaveBeenCalled();
  });

  it('nunca pede permissão nem registra token ao fazer login', async () => {
    await expect(solicitarPermissaoERegistrarAsync('prop-1')).resolves.toBe('indisponivel');
    expect(Notifications.requestPermissionsAsync).not.toHaveBeenCalled();
    expect(api.post).not.toHaveBeenCalled();
  });

  it('não reenvia registro nem atualiza token', async () => {
    await reenviarRegistroSeJaPermitidoAsync('prop-1');
    await atualizarTokenAsync('token-novo');
    expect(api.post).not.toHaveBeenCalled();
    expect(api.put).not.toHaveBeenCalled();
  });
});
