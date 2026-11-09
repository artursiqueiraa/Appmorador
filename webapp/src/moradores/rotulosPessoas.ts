import type { MoradorResponse, Parentesco, TipoUnidade, TipoVeiculo } from '../api/types';
import type { TipoPropriedade } from '../components/TipoPropriedadeSelector';

export const PARENTESCOS: { valor: Parentesco; rotulo: string }[] = [
  { valor: 'Conjuge', rotulo: 'Cônjuge' },
  { valor: 'Filho', rotulo: 'Filho(a)' },
  { valor: 'PaiOuMae', rotulo: 'Pai ou mãe' },
  { valor: 'Irmao', rotulo: 'Irmão(ã)' },
  { valor: 'AvoOuAva', rotulo: 'Avô ou avó' },
  { valor: 'Outro', rotulo: 'Outro' },
];

export const TIPOS_VEICULO: { valor: TipoVeiculo; rotulo: string }[] = [
  { valor: 'Carro', rotulo: 'Carro' },
  { valor: 'Moto', rotulo: 'Moto' },
  { valor: 'Caminhonete', rotulo: 'Caminhonete' },
  { valor: 'Van', rotulo: 'Van' },
  { valor: 'Outro', rotulo: 'Outro' },
];

/** "Titular", "Dependente, filho(a)". */
export function descreverPapel(p: Pick<MoradorResponse, 'tipo' | 'parentesco'>): string {
  if (p.tipo !== 'Dependente') return 'Titular';
  const parentesco = PARENTESCOS.find((x) => x.valor === p.parentesco)?.rotulo;
  return parentesco ? `Dependente, ${parentesco.toLowerCase()}` : 'Dependente';
}

export function iniciais(nome: string): string {
  return nome
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((parte) => parte[0]?.toUpperCase() ?? '')
    .join('');
}

/** Unidade criada automaticamente quando a propriedade ainda não tem nenhuma (o morador não precisa saber o que é "unidade"). */
export function unidadePadrao(tipo: TipoPropriedade): { tipo: TipoUnidade; identificacao: string } {
  if (tipo === 'Comercial') return { tipo: 'Loja', identificacao: 'Principal' };
  return { tipo: 'Casa', identificacao: 'Principal' };
}

/** Placa brasileira (antiga AAA9999 ou Mercosul AAA9A99), sem traço/espaço. */
export function placaValida(placa: string): boolean {
  return /^[A-Z]{3}\d[A-Z0-9]\d{2}$/.test(placa.replace(/[\s-]/g, '').toUpperCase());
}
