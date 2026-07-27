/**
 * Sprint 20 (ADR 0024) — cobre o wording honesto: sem monitoramento contínuo, a
 * interface nunca pode afirmar "Offline desde X" (um instante que não conhecemos
 * de verdade) — só "última imagem há X", verdadeiro tanto para Online quanto
 * Offline.
 */
import { rotuloStatusBadge, rotuloStatusDetalhado, rotuloTimestampCurto } from '../cameraLabels';

describe('rotuloStatusBadge', () => {
  it('Online mostra o emoji verde', () => {
    expect(rotuloStatusBadge('Online')).toBe('🟢 Online');
  });

  it('Offline mostra o emoji vermelho', () => {
    expect(rotuloStatusBadge('Offline')).toBe('🔴 Offline');
  });

  it('Desconhecido mostra o emoji neutro', () => {
    expect(rotuloStatusBadge('Desconhecido')).toBe('⚪ Desconhecido');
  });
});

describe('rotuloTimestampCurto', () => {
  it('sem ultimaVezVistaUtc mostra "Sem imagem"', () => {
    expect(rotuloTimestampCurto({ ultimaVezVistaUtc: null })).toBe('Sem imagem');
  });

  it('com ultimaVezVistaUtc recente mostra tempo relativo', () => {
    const doisMinutosAtras = new Date(Date.now() - 2 * 60_000).toISOString();
    expect(rotuloTimestampCurto({ ultimaVezVistaUtc: doisMinutosAtras })).toContain('minuto');
  });
});

describe('rotuloStatusDetalhado', () => {
  it('Online com timestamp nunca menciona "desde" — só "última imagem"', () => {
    const agora = new Date().toISOString();
    const rotulo = rotuloStatusDetalhado('Online', agora);
    expect(rotulo).toContain('Online');
    expect(rotulo).toContain('última imagem');
    expect(rotulo.toLowerCase()).not.toContain('desde');
  });

  it('Offline com timestamp diz "última imagem", nunca "offline desde"', () => {
    const duasHorasAtras = new Date(Date.now() - 2 * 60 * 60_000).toISOString();
    const rotulo = rotuloStatusDetalhado('Offline', duasHorasAtras);
    expect(rotulo).toContain('Offline');
    expect(rotulo).toContain('última imagem');
    expect(rotulo.toLowerCase()).not.toContain('offline desde');
  });

  it('Offline sem nenhum timestamp nunca finge saber uma hora exata', () => {
    const rotulo = rotuloStatusDetalhado('Offline', null);
    expect(rotulo).toBe('Offline — nenhuma imagem disponível ainda');
  });

  it('Desconhecido sem timestamp', () => {
    expect(rotuloStatusDetalhado('Desconhecido', null)).toBe('Nenhuma imagem disponível ainda');
  });
});
