/** Akteck Seg (ADR 0035) — vocabulário de `TipoPropriedade` compartilhado entre Dashboard, Clientes e Nova instalação. */
export type TipoPropriedade = 'Residencial' | 'Comercial' | 'Condominio' | 'Rural' | 'Outro';

export const TIPOS_PROPRIEDADE: { valor: TipoPropriedade; rotulo: string; descricao: string }[] = [
  { valor: 'Residencial', rotulo: 'Residencial', descricao: 'Casa ou apartamento' },
  { valor: 'Comercial', rotulo: 'Comercial', descricao: 'Lojas, escritórios, clínicas e pequenos negócios' },
  { valor: 'Condominio', rotulo: 'Condomínio', descricao: 'Áreas comuns de um condomínio' },
  { valor: 'Rural', rotulo: 'Rural', descricao: 'Sítios, chácaras e fazendas' },
  { valor: 'Outro', rotulo: 'Outro', descricao: 'Não se encaixa nas opções acima' },
];

export const ROTULO_TIPO_PROPRIEDADE: Record<string, string> = Object.fromEntries(TIPOS_PROPRIEDADE.map((t) => [t.valor, t.rotulo]));
