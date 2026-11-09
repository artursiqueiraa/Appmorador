/**
 * Sprint 27 (ADR 0043) — como o app obtém a confirmação de desarmar/abrir.
 */
import { ApiError } from '../../api/client';
import { confirmarComando, invalidarSituacaoPin, registrarHostConfirmacao, type PedidoConfirmacao } from '../confirmarComando';
import { biometria } from '../biometria';

jest.mock('../../api/client', () => {
  class ApiErrorMock extends Error {
    codigo?: string;
    constructor(_s: number, mensagem: string, _t?: string, codigoErro?: string) {
      super(mensagem);
      this.codigo = codigoErro;
    }
  }
  return { ApiError: ApiErrorMock, api: { get: jest.fn() } };
});

jest.mock('../../navigation/navigationRef', () => ({
  navigationRef: { isReady: () => true, navigate: jest.fn() },
}));

jest.mock('../biometria', () => ({
  biometria: { obterPin: jest.fn(), desativarConfirmacao: jest.fn() },
}));

const { api } = jest.requireMock('../../api/client') as { api: { get: jest.Mock } };
const { navigationRef } = jest.requireMock('../../navigation/navigationRef') as { navigationRef: { navigate: jest.Mock } };

const erroPin = (codigo: string, mensagem = 'PIN incorreto. Restam 4 tentativas.') => new ApiError(428, mensagem, mensagem, codigo);

/** Host de teste: responde cada pedido com o próximo valor da fila e guarda o que viu. */
function hostQueResponde(respostas: (string | null)[]) {
  const vistos: PedidoConfirmacao[] = [];
  registrarHostConfirmacao((pedido) => {
    if (!pedido) return;
    vistos.push(pedido);
    const resposta = respostas.shift() ?? null;
    (pedido.responder as (v: string | null) => void)(resposta);
  });
  return vistos;
}

beforeEach(() => {
  jest.clearAllMocks();
  invalidarSituacaoPin();
  (biometria.obterPin as jest.Mock).mockResolvedValue(null);
});

describe('confirmarComando', () => {
  it('sem PIN criado: convida uma vez e, ao continuar, executa sem PIN mas com chave', async () => {
    api.get.mockResolvedValue({ definido: false });
    const vistos = hostQueResponde(['continuar']);
    const executar = jest.fn().mockResolvedValue('ok');

    const r = await confirmarComando('Desarmar o alarme', executar);

    expect(r).toBe('ok');
    expect(vistos.map((v) => v.tipo)).toEqual(['convite']);
    expect(executar.mock.calls[0][0]['Idempotency-Key']).toBeTruthy();
    expect(executar.mock.calls[0][0]['X-Confirmacao-Pin']).toBeUndefined();
  });

  it('sem PIN criado: "Criar PIN agora" leva para Segurança e não executa', async () => {
    api.get.mockResolvedValue({ definido: false });
    hostQueResponde(['criar']);
    const executar = jest.fn();

    const r = await confirmarComando('Abrir portão', executar);

    expect(r).toBeNull();
    expect(executar).not.toHaveBeenCalled();
    expect(navigationRef.navigate).toHaveBeenCalledWith('Seguranca');
  });

  it('PIN errado pede de novo com a mensagem do servidor e reaproveita a mesma chave', async () => {
    api.get.mockResolvedValue({ definido: true });
    const vistos = hostQueResponde(['1111', '2580']);
    const executar = jest.fn().mockRejectedValueOnce(erroPin('PIN_INCORRETO')).mockResolvedValueOnce('aberto');

    const r = await confirmarComando('Abrir portão', executar);

    expect(r).toBe('aberto');
    expect(vistos).toHaveLength(2);
    expect((vistos[1] as { erro?: string }).erro).toBe('PIN incorreto. Restam 4 tentativas.');
    expect(executar.mock.calls[1][0]['X-Confirmacao-Pin']).toBe('2580');
    expect(executar.mock.calls[0][0]['Idempotency-Key']).toBe(executar.mock.calls[1][0]['Idempotency-Key']);
  });

  it('cancelar a folha de PIN não executa', async () => {
    api.get.mockResolvedValue({ definido: true });
    hostQueResponde([null]);
    const executar = jest.fn();

    expect(await confirmarComando('Desarmar', executar)).toBeNull();
    expect(executar).not.toHaveBeenCalled();
  });

  it('PIN bloqueado é repassado para a tela', async () => {
    api.get.mockResolvedValue({ definido: true });
    hostQueResponde(['2580']);
    const executar = jest.fn().mockRejectedValue(erroPin('PIN_BLOQUEADO', 'Muitas tentativas erradas.'));

    await expect(confirmarComando('Desarmar', executar)).rejects.toThrow('Muitas tentativas erradas.');
  });

  it('com digital/rosto: manda o PIN guardado marcando biometria, sem abrir a folha', async () => {
    api.get.mockResolvedValue({ definido: true });
    (biometria.obterPin as jest.Mock).mockResolvedValue('2580');
    const vistos = hostQueResponde([]);
    const executar = jest.fn().mockResolvedValue('ok');

    await confirmarComando('Desarmar', executar);

    expect(vistos).toHaveLength(0);
    expect(executar.mock.calls[0][0]).toMatchObject({ 'X-Confirmacao-Pin': '2580', 'X-Confirmacao-Biometria': '1' });
  });

  it('PIN guardado desatualizado: esquece o guardado e pede pela folha', async () => {
    api.get.mockResolvedValue({ definido: true });
    (biometria.obterPin as jest.Mock).mockResolvedValue('0000');
    hostQueResponde(['2580']);
    const executar = jest.fn().mockRejectedValueOnce(erroPin('PIN_INCORRETO')).mockResolvedValueOnce('ok');

    expect(await confirmarComando('Desarmar', executar)).toBe('ok');
    expect(biometria.desativarConfirmacao).toHaveBeenCalled();
  });
});
