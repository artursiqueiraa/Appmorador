/**
 * Sprint 19 (ADR 0023, Fase 5) — cobre o mapeamento de deep link: a `acao` que
 * vem no payload da notificação (ver `NotificationDispatcher` no backend) precisa
 * navegar para a tela certa, e uma `acao` desconhecida/ausente nunca deve lançar
 * nem navegar para lugar nenhum.
 */
import { navigationRef } from '../../navigation/navigationRef';
import { navegarParaAcao } from '../PushNotificationProvider';

jest.mock('expo-notifications', () => ({
  setNotificationHandler: jest.fn(),
  addPushTokenListener: jest.fn().mockReturnValue({ remove: jest.fn() }),
  addNotificationResponseReceivedListener: jest.fn().mockReturnValue({ remove: jest.fn() }),
  getLastNotificationResponse: jest.fn().mockReturnValue(null),
}));

jest.mock('../../navigation/navigationRef', () => ({
  navigationRef: {
    isReady: jest.fn().mockReturnValue(true),
    navigate: jest.fn(),
  },
}));

// A própria PushNotificationProvider importa useAuth/registerBeforeLogoutHook, que
// puxam api/client -> config/env (que exige EXPO_PUBLIC_API_URL em runtime) — sem
// relação nenhuma com o que este teste cobre (só o mapeamento de deep link).
jest.mock('../../auth/AuthContext', () => ({
  useAuth: jest.fn(),
  registerBeforeLogoutHook: jest.fn(),
}));

jest.mock('../pushChannels', () => ({
  configurarCanaisAndroidAsync: jest.fn().mockResolvedValue(undefined),
}));

jest.mock('../pushService', () => ({
  atualizarTokenAsync: jest.fn(),
  desregistrarAsync: jest.fn(),
  reenviarRegistroSeJaPermitidoAsync: jest.fn(),
  solicitarPermissaoERegistrarAsync: jest.fn(),
}));

describe('navegarParaAcao', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it('ABRIR_APP_HISTORICO navega para a tela Eventos', () => {
    navegarParaAcao('ABRIR_APP_HISTORICO');
    expect(navigationRef.navigate).toHaveBeenCalledWith('Eventos');
  });

  it('ABRIR_APP_INICIO navega para a aba Inicio dentro de MainTabs', () => {
    navegarParaAcao('ABRIR_APP_INICIO');
    expect(navigationRef.navigate).toHaveBeenCalledWith('MainTabs', { screen: 'Inicio' });
  });

  it('ABRIR_APP_ACESSOS navega para a aba Acessos dentro de MainTabs', () => {
    navegarParaAcao('ABRIR_APP_ACESSOS');
    expect(navigationRef.navigate).toHaveBeenCalledWith('MainTabs', { screen: 'Acessos' });
  });

  it('acao desconhecida nunca navega nem lanca', () => {
    expect(() => navegarParaAcao('ACAO_QUE_NAO_EXISTE')).not.toThrow();
    expect(navigationRef.navigate).not.toHaveBeenCalled();
  });

  it('acao ausente (undefined) nunca navega nem lanca', () => {
    expect(() => navegarParaAcao(undefined)).not.toThrow();
    expect(navigationRef.navigate).not.toHaveBeenCalled();
  });

  it('quando a navegacao ainda nao esta pronta, tenta de novo em vez de descartar silenciosamente', () => {
    jest.useFakeTimers();
    (navigationRef.isReady as jest.Mock).mockReturnValue(false);

    navegarParaAcao('ABRIR_APP_HISTORICO');
    expect(navigationRef.navigate).not.toHaveBeenCalled();

    (navigationRef.isReady as jest.Mock).mockReturnValue(true);
    jest.advanceTimersByTime(300);

    expect(navigationRef.navigate).toHaveBeenCalledWith('Eventos');
    jest.useRealTimers();
  });
});
