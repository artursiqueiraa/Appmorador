import { descreverPapel, iniciais, placaValida, unidadePadrao } from '../rotulosPessoas';

describe('rotulosPessoas', () => {
  it('descreve titular e dependente com parentesco', () => {
    expect(descreverPapel({ tipo: 'Titular', parentesco: null })).toBe('Titular');
    expect(descreverPapel({ tipo: 'Dependente', parentesco: 'Filho' })).toBe('Dependente, filho(a)');
    expect(descreverPapel({ tipo: 'Dependente', parentesco: null })).toBe('Dependente');
  });

  it('aceita placa antiga e Mercosul, com ou sem traço', () => {
    expect(placaValida('ABC1234')).toBe(true);
    expect(placaValida('abc-1d23')).toBe(true);
    expect(placaValida('AB12345')).toBe(false);
    expect(placaValida('')).toBe(false);
  });

  it('gera iniciais com no máximo duas letras', () => {
    expect(iniciais('Maria da Silva')).toBe('MD');
    expect(iniciais('  ana ')).toBe('A');
  });

  it('cria unidade "Principal" coerente com o tipo da propriedade', () => {
    expect(unidadePadrao('Residencial')).toEqual({ tipo: 'Casa', identificacao: 'Principal' });
    expect(unidadePadrao('Comercial')).toEqual({ tipo: 'Loja', identificacao: 'Principal' });
  });
});
