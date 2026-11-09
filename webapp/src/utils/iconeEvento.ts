import { Bell, DoorOpen, Shield, ShieldAlert, ShieldOff, Siren, Video, type LucideIcon } from 'lucide-react-native';

export type TomEvento = 'marca' | 'perigo' | 'atencao' | 'neutro';

/**
 * Akteck Seg (ADR 0037) — ícone e tom de um evento a partir do texto que o backend já devolve
 * (`EventoResponse` não tem categoria). Grave (`destaque`) é sempre vermelho.
 */
export function iconeEvento(titulo: string, destaque: boolean): { icone: LucideIcon; tom: TomEvento } {
  const t = titulo.toLowerCase();
  if (t.includes('negad')) return { icone: ShieldAlert, tom: 'perigo' };
  if (t.includes('disparo') || t.includes('disparad') || t.includes('alarme dispar')) return { icone: Siren, tom: 'perigo' };
  if (t.includes('desarm')) return { icone: ShieldOff, tom: destaque ? 'perigo' : 'marca' };
  if (t.includes('armad') || t.includes('arme')) return { icone: Shield, tom: destaque ? 'perigo' : 'marca' };
  if (t.includes('câmera') || t.includes('camera') || t.includes('movimento')) return { icone: Video, tom: destaque ? 'perigo' : 'marca' };
  if (t.includes('acesso') || t.includes('porta') || t.includes('portão') || t.includes('liberad')) return { icone: DoorOpen, tom: destaque ? 'perigo' : 'marca' };
  return { icone: Bell, tom: destaque ? 'perigo' : 'neutro' };
}

/** "20:32" para hoje; "03/11, 20:32" para outros dias. */
export function horaEvento(utc: string): string {
  const data = new Date(utc);
  const hoje = new Date();
  const hora = data.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  return data.toDateString() === hoje.toDateString() ? hora : `${data.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })}, ${hora}`;
}
