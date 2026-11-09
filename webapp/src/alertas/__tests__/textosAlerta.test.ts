/**
 * Sprint 24 (ADR 0040) — frase de situação da tela de alerta e formato de telefone.
 */
import type { AlertaCriticoResponse } from '../../api/types';
import { formatarTelefone } from '../servicoAlertas';
import { situacaoEscalonamento } from '../textosAlerta';

// servicoAlertas importa a API (que exige EXPO_PUBLIC_API_URL); só a função pura interessa aqui.
// jest.mock é içado para antes dos imports.
jest.mock('../../api/client', () => ({ api: {} }));

const base: AlertaCriticoResponse = {
  id: 'a1',
  propriedadeId: 'p1',
  tipo: 'Panico',
  titulo: 'Pânico acionado',
  descricao: 'Botão de pânico acionado pelo app',
  criadoEmUtc: '2026-11-08T12:00:00Z',
  confirmado: false,
  nivelEscalonamento: 'Dono',
  acionadoPeloApp: true,
};

describe('situacaoEscalonamento', () => {
  it('confirmado mostra quem confirmou', () => {
    expect(situacaoEscalonamento({ ...base, confirmado: true, confirmadoPorNome: 'Fernanda' })).toMatch(/^Confirmado por Fernanda/);
  });

  it('contatos ja avisados diz que receberam SMS', () => {
    expect(situacaoEscalonamento({ ...base, escalonadoContatosEmUtc: '2026-11-08T12:02:00Z' })).toMatch(/contatos de emergência receberam um SMS/);
  });

  it('aguardando, com prazo, avisa quando o proximo sera chamado', () => {
    expect(situacaoEscalonamento({ ...base, proximoEscalonamentoEmUtc: '2026-11-08T12:02:00Z' })).toMatch(/^Se ninguém confirmar até/);
  });

  it('sem prazo e sem ninguem avisado pede para confirmar', () => {
    expect(situacaoEscalonamento(base)).toMatch(/Estou ciente/);
  });

  it('nunca usa vocabulario tecnico', () => {
    const textos = [
      situacaoEscalonamento(base),
      situacaoEscalonamento({ ...base, escalonadoMembrosEmUtc: '2026-11-08T12:02:00Z', proximoEscalonamentoEmUtc: '2026-11-08T12:04:00Z' }),
    ];
    textos.forEach((t) => expect(t).not.toMatch(/escalon|nível|nivel/i));
  });
});

describe('formatarTelefone', () => {
  it.each([
    ['11987654321', '(11) 98765-4321'],
    ['1134567890', '(11) 3456-7890'],
    ['5511987654321', '(11) 98765-4321'],
    ['123', '123'],
  ])('%s → %s', (entrada, esperado) => {
    expect(formatarTelefone(entrada)).toBe(esperado);
  });
});
