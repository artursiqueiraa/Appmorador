import type { DiagnosticoEquipamento } from '../diagnostico/types';

export interface PropriedadeEmAtencao {
  propriedadeId: string;
  propriedadeNome: string;
  offline: number;
  comDefeito: number;
  comProblema: number;
  ultimoSinalUtc: string | null;
}

/** Precisa de atenção: offline, marcado como defeituoso ou com problema ativo reportado pela central. */
export function agruparPorPropriedade(itens: DiagnosticoEquipamento[]): PropriedadeEmAtencao[] {
  const mapa = new Map<string, PropriedadeEmAtencao>();
  for (const eq of itens) {
    const offline = eq.status === 'Offline';
    const defeito = eq.estadoOperacional === 'Defeituoso';
    const problema = Boolean(eq.temProblemaAtivo);
    if (!offline && !defeito && !problema) continue;

    const atual = mapa.get(eq.propriedadeId) ?? {
      propriedadeId: eq.propriedadeId,
      propriedadeNome: eq.propriedadeNome,
      offline: 0,
      comDefeito: 0,
      comProblema: 0,
      ultimoSinalUtc: null,
    };
    if (offline) atual.offline += 1;
    if (defeito) atual.comDefeito += 1;
    if (problema) atual.comProblema += 1;
    if (eq.ultimoPingUtc && (!atual.ultimoSinalUtc || eq.ultimoPingUtc > atual.ultimoSinalUtc)) {
      atual.ultimoSinalUtc = eq.ultimoPingUtc;
    }
    mapa.set(eq.propriedadeId, atual);
  }
  const peso = (p: PropriedadeEmAtencao) => p.offline * 2 + p.comDefeito * 2 + p.comProblema;
  return [...mapa.values()].sort((a, b) => peso(b) - peso(a));
}
