/**
 * Sprint 19 (ADR 0023) — cobre o ciclo de vida do token de push do lado do
 * Mobile: registrar (token novo), reenviar sem re-pedir permissão, atualizar
 * (refresh de token) e desregistrar (logout) — sempre best-effort, nunca
 * lançando para quem chama.
 */
import { api } from '../../api/client';
import * as Notifications from 'expo-notifications';
import {
  atualizarPreferenciasAsync,
  atualizarTokenAsync,
  desregistrarAsync,
  obterStatusPermissaoAsync,
  reenviarRegistroSeJaPermitidoAsync,
  solicitarPermissaoERegistrarAsync,
} from '../pushService';
import { obterDispositivoPushId, salvarDispositivoPushId, salvarPreferenciasLocais } from '../pushDeviceStorage';

jest.mock('../../api/client', () => ({
  api: {
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
  },
}));

jest.mock('expo-notifications', () => ({
  getPermissionsAsync: jest.fn(),
  requestPermissionsAsync: jest.fn(),
  getDevicePushTokenAsync: jest.fn(),
}));

jest.mock('expo-constants', () => ({
  expoConfig: { version: '1.0.0' },
}));

jest.mock('../pushDeviceStorage', () => ({
  obterDispositivoPushId: jest.fn(),
  salvarDispositivoPushId: jest.fn(),
  limparDispositivoPushId: jest.fn(),
  salvarPreferenciasLocais: jest.fn(),
}));

describe('pushService', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  describe('obterStatusPermissaoAsync', () => {
    it('traduz "granted" para "concedida"', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'granted' });
      await expect(obterStatusPermissaoAsync()).resolves.toBe('concedida');
    });

    it('traduz "denied" para "negada"', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'denied' });
      await expect(obterStatusPermissaoAsync()).resolves.toBe('negada');
    });

    it('traduz "undetermined" para "nao-solicitada"', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'undetermined' });
      await expect(obterStatusPermissaoAsync()).resolves.toBe('nao-solicitada');
    });
  });

  describe('solicitarPermissaoERegistrarAsync', () => {
    it('quando nunca foi solicitada e o usuario concede, registra o token', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'undetermined' });
      (Notifications.requestPermissionsAsync as jest.Mock).mockResolvedValue({ granted: true });
      (Notifications.getDevicePushTokenAsync as jest.Mock).mockResolvedValue({ data: 'token-abc' });
      (api.post as jest.Mock).mockResolvedValue({ id: 'dispositivo-1' });

      const status = await solicitarPermissaoERegistrarAsync('propriedade-1');

      expect(status).toBe('concedida');
      expect(api.post).toHaveBeenCalledWith(
        '/api/dispositivos-push',
        expect.objectContaining({ propriedadeId: 'propriedade-1', token: 'token-abc' }),
      );
      expect(salvarDispositivoPushId).toHaveBeenCalledWith('dispositivo-1');
    });

    it('quando o usuario nega, nunca chama a api e nunca insiste numa segunda chamada', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'undetermined' });
      (Notifications.requestPermissionsAsync as jest.Mock).mockResolvedValue({ granted: false });

      const status = await solicitarPermissaoERegistrarAsync();

      expect(status).toBe('negada');
      expect(api.post).not.toHaveBeenCalled();
    });

    it('quando ja foi negada anteriormente, nunca exibe o dialogo nativo de novo', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'denied' });

      await solicitarPermissaoERegistrarAsync();

      expect(Notifications.requestPermissionsAsync).not.toHaveBeenCalled();
    });

    it('falha ao obter o token nunca lanca — apenas nao registra', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'granted' });
      (Notifications.getDevicePushTokenAsync as jest.Mock).mockRejectedValue(new Error('sem Firebase configurado'));

      await expect(solicitarPermissaoERegistrarAsync()).resolves.toBe('concedida');
      expect(api.post).not.toHaveBeenCalled();
    });
  });

  describe('reenviarRegistroSeJaPermitidoAsync', () => {
    it('nao solicita permissao, so reenvia quando ja concedida', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'granted' });
      (Notifications.getDevicePushTokenAsync as jest.Mock).mockResolvedValue({ data: 'token-abc' });
      (api.post as jest.Mock).mockResolvedValue({ id: 'dispositivo-1' });

      await reenviarRegistroSeJaPermitidoAsync('propriedade-2');

      expect(Notifications.requestPermissionsAsync).not.toHaveBeenCalled();
      expect(api.post).toHaveBeenCalledWith('/api/dispositivos-push', expect.objectContaining({ propriedadeId: 'propriedade-2' }));
    });

    it('nao faz nada quando a permissao nunca foi concedida', async () => {
      (Notifications.getPermissionsAsync as jest.Mock).mockResolvedValue({ status: 'undetermined' });

      await reenviarRegistroSeJaPermitidoAsync('propriedade-2');

      expect(api.post).not.toHaveBeenCalled();
    });
  });

  describe('atualizarTokenAsync', () => {
    it('sem dispositivo registrado localmente, nao chama a api', async () => {
      (obterDispositivoPushId as jest.Mock).mockResolvedValue(null);

      await atualizarTokenAsync('token-novo');

      expect(api.put).not.toHaveBeenCalled();
    });

    it('com dispositivo registrado, envia o novo token', async () => {
      (obterDispositivoPushId as jest.Mock).mockResolvedValue('dispositivo-1');
      (api.put as jest.Mock).mockResolvedValue(undefined);

      await atualizarTokenAsync('token-novo');

      expect(api.put).toHaveBeenCalledWith('/api/dispositivos-push/dispositivo-1', { token: 'token-novo' });
    });

    it('falha da api nunca lanca (best-effort)', async () => {
      (obterDispositivoPushId as jest.Mock).mockResolvedValue('dispositivo-1');
      (api.put as jest.Mock).mockRejectedValue(new Error('sem rede'));

      await expect(atualizarTokenAsync('token-novo')).resolves.toBeUndefined();
    });
  });

  describe('atualizarPreferenciasAsync', () => {
    it('sem dispositivo registrado, retorna null sem chamar a api', async () => {
      (obterDispositivoPushId as jest.Mock).mockResolvedValue(null);

      const resultado = await atualizarPreferenciasAsync({ notificarAlertas: true, notificarAtividades: false, notificarGeral: true });

      expect(resultado).toBeNull();
      expect(api.put).not.toHaveBeenCalled();
    });

    it('com dispositivo registrado, envia preferencias e espelha localmente', async () => {
      (obterDispositivoPushId as jest.Mock).mockResolvedValue('dispositivo-1');
      const preferencias = { notificarAlertas: true, notificarAtividades: false, notificarGeral: true };
      (api.put as jest.Mock).mockResolvedValue({ id: 'dispositivo-1', ...preferencias });

      await atualizarPreferenciasAsync(preferencias);

      expect(api.put).toHaveBeenCalledWith('/api/dispositivos-push/dispositivo-1/preferencias', preferencias);
      expect(salvarPreferenciasLocais).toHaveBeenCalledWith(preferencias);
    });
  });

  describe('desregistrarAsync', () => {
    it('sem dispositivo registrado, nao chama a api', async () => {
      (obterDispositivoPushId as jest.Mock).mockResolvedValue(null);

      await desregistrarAsync();

      expect(api.delete).not.toHaveBeenCalled();
    });

    it('com dispositivo registrado, chama DELETE e limpa o id local mesmo se a api falhar', async () => {
      (obterDispositivoPushId as jest.Mock).mockResolvedValue('dispositivo-1');
      (api.delete as jest.Mock).mockRejectedValue(new Error('sem rede'));
      const { limparDispositivoPushId } = jest.requireMock('../pushDeviceStorage');

      await desregistrarAsync();

      expect(api.delete).toHaveBeenCalledWith('/api/dispositivos-push/dispositivo-1');
      expect(limparDispositivoPushId).toHaveBeenCalled();
    });
  });
});
