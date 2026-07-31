import { describe, expect, it } from 'vitest';
import { AxiosError } from 'axios';
import { extrairMensagemErro } from '../httpClient';

function criarAxiosError(status: number, data?: unknown): AxiosError {
  const erro = new AxiosError('erro técnico', 'ERR', undefined, undefined, {
    status,
    data,
    statusText: '',
    headers: {},
    config: {} as never,
  });
  return erro;
}

describe('extrairMensagemErro', () => {
  it('extrai o campo `error` do corpo da resposta quando presente', () => {
    const erro = criarAxiosError(400, { error: 'E-mail ou senha inválidos.' });

    expect(extrairMensagemErro(erro)).toBe('E-mail ou senha inválidos.');
  });

  it('sem resposta do servidor: mensagem de conexão', () => {
    const erro = new AxiosError('Network Error', 'ERR_NETWORK');

    expect(extrairMensagemErro(erro)).toBe('Não foi possível conectar ao servidor.');
  });

  it('erro não-Axios: usa a mensagem padrão informada', () => {
    expect(extrairMensagemErro(new Error('algo interno'), 'Mensagem padrão customizada')).toBe(
      'Mensagem padrão customizada',
    );
  });

  it('AxiosError sem corpo `error`: usa a mensagem padrão', () => {
    const erro = criarAxiosError(500, {});

    expect(extrairMensagemErro(erro, 'Algo deu errado.')).toBe('Algo deu errado.');
  });
});
