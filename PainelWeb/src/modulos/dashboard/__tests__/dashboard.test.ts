import { describe, expect, it } from 'vitest';
import { agruparPorPropriedade } from '../agruparAtencao';
import { descreverAtividade } from '../rotulosDashboard';
import type { DiagnosticoEquipamento } from '../../diagnostico/types';
import type { AuditoriaMasterItem } from '../../../types/api';

const equipamento = (parcial: Partial<DiagnosticoEquipamento>): DiagnosticoEquipamento => ({
  equipamentoId: Math.random().toString(),
  equipamentoNome: 'Central',
  fabricante: 'Jfl',
  propriedadeId: 'p1',
  propriedadeNome: 'Casa',
  status: 'Online',
  estadoOperacional: 'Ativo',
  quantidadeEventosRecentes: 0,
  ...parcial,
});

describe('agruparPorPropriedade', () => {
  it('ignora propriedades em que tudo está online e ativo', () => {
    expect(agruparPorPropriedade([equipamento({}), equipamento({})])).toEqual([]);
  });

  it('conta offline, defeito e alerta por propriedade e ordena pelo mais grave', () => {
    const fila = agruparPorPropriedade([
      equipamento({ propriedadeId: 'leve', propriedadeNome: 'Leve', temProblemaAtivo: true }),
      equipamento({ propriedadeId: 'grave', propriedadeNome: 'Grave', status: 'Offline' }),
      equipamento({ propriedadeId: 'grave', propriedadeNome: 'Grave', estadoOperacional: 'Defeituoso' }),
    ]);
    expect(fila.map((p) => p.propriedadeNome)).toEqual(['Grave', 'Leve']);
    expect(fila[0]).toMatchObject({ offline: 1, comDefeito: 1, comProblema: 0 });
  });

  it('guarda o sinal mais recente da propriedade', () => {
    const [p] = agruparPorPropriedade([
      equipamento({ status: 'Offline', ultimoPingUtc: '2026-07-01T10:00:00Z' }),
      equipamento({ status: 'Offline', ultimoPingUtc: '2026-07-03T10:00:00Z' }),
    ]);
    expect(p.ultimoSinalUtc).toBe('2026-07-03T10:00:00Z');
  });
});

describe('descreverAtividade', () => {
  const item = (parcial: Partial<AuditoriaMasterItem>): AuditoriaMasterItem => ({
    id: '1',
    usuarioId: 'u',
    usuarioNome: 'Carlos Henrique',
    acao: 'Editar',
    dataHoraUtc: '2026-11-02T10:00:00Z',
    ...parcial,
  });

  it('descreve em linguagem humana, sem o nome do enum', () => {
    expect(descreverAtividade(item({ entidade: 'Equipamento' }))).toBe('Carlos Henrique editou um equipamento');
  });

  it('acesso negado sem usuário vira "Usuário não identificado" e esconde "Endpoint"', () => {
    expect(descreverAtividade(item({ usuarioNome: '(não resolvido)', acao: 'FalhaAutorizacao', entidade: 'Endpoint' }))).toBe(
      'Usuário não identificado teve um acesso negado',
    );
  });
});
