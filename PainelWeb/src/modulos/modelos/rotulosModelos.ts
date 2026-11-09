import type { FabricanteEquipamento } from '../../compartilhado/tipos/equipamento';
import type { CapacidadeEquipamento } from './types';

export const FABRICANTES: { valor: FabricanteEquipamento; rotulo: string }[] = [
  { valor: 'ControlId', rotulo: 'Control iD' },
  { valor: 'Jfl', rotulo: 'JFL' },
  { valor: 'Intelbras', rotulo: 'Intelbras' },
];

export const rotuloFabricante = (f: FabricanteEquipamento) => FABRICANTES.find((x) => x.valor === f)?.rotulo ?? f;

/** Ordem e texto de cada capacidade (o que o equipamento sabe fazer). */
export const CAPACIDADES: { valor: CapacidadeEquipamento; rotulo: string; descricao: string }[] = [
  { valor: 'Face', rotulo: 'Reconhecimento facial', descricao: 'Libera acesso pelo rosto.' },
  { valor: 'Tag', rotulo: 'Tag / cartão', descricao: 'Libera acesso por tag ou cartão de proximidade.' },
  { valor: 'QrCode', rotulo: 'QR Code', descricao: 'Libera acesso por código temporário.' },
  { valor: 'Senha', rotulo: 'Senha', descricao: 'Libera acesso por senha numérica.' },
  { valor: 'Armar', rotulo: 'Armar alarme', descricao: 'Aceita comando para armar.' },
  { valor: 'Desarmar', rotulo: 'Desarmar alarme', descricao: 'Aceita comando para desarmar.' },
  { valor: 'Pgm', rotulo: 'Saída PGM', descricao: 'Aciona portão, luz ou outro dispositivo.' },
  { valor: 'Streaming', rotulo: 'Vídeo ao vivo', descricao: 'Transmite imagem em tempo real.' },
  { valor: 'Ptz', rotulo: 'Movimento de câmera', descricao: 'Câmera gira e aproxima (PTZ).' },
];

export const rotuloCapacidade = (c: CapacidadeEquipamento) => CAPACIDADES.find((x) => x.valor === c)?.rotulo ?? c;
