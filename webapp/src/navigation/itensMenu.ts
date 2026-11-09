import { Bell, Car, CircleUserRound, FileText, DoorOpen, History, Home, KeyRound, Package, Repeat, Settings, ShieldAlert, UserRound, Users, type LucideIcon } from 'lucide-react-native';
import { podeGerenciarPessoas } from '../pessoas/papeis';
import type { PropriedadeResponse } from '../api/types';
import { navigationRef } from './navigationRef';

export interface ItemMenu {
  id: string;
  rotulo: string;
  icone: LucideIcon;
  /** Seção do menu lateral. */
  grupo: 'Casa' | 'Conta';
  abrir: (propriedade: PropriedadeResponse, acoes: { trocarPropriedade: () => void }) => void;
  /** Sprint 25 — esconde o item conforme o papel/tipo da propriedade (padrão: sempre visível). */
  visivel?: (propriedade: PropriedadeResponse) => boolean;
}

/** Sprint 25 — Vagas e Entregas só fazem sentido em condomínio (dívida 58). */
const soCondominio = (p: PropriedadeResponse) => p.tipo === 'Condominio';

/**
 * Akteck Seg (ADR 0038) — o que não cabe nas 4 abas de baixo (Início, Câmeras, Alarme,
 * Moradores) fica no menu lateral: no celular ele abre pelo botão "Menu"; no computador fica
 * sempre visível na barra lateral. Fonte única para os dois.
 */
export const ITENS_MENU: ItemMenu[] = [
  { id: 'eventos', rotulo: 'Histórico de eventos', icone: History, grupo: 'Casa', abrir: () => navigationRef.navigate('Eventos') },
  {
    id: 'portoes',
    rotulo: 'Portões e portas',
    icone: DoorOpen,
    grupo: 'Casa',
    abrir: (p) => navigationRef.navigate('PontosAcesso', { propriedadeId: p.id, nomePropriedade: p.nome }),
  },
  { id: 'visitantes', rotulo: 'Visitantes', icone: UserRound, grupo: 'Casa', abrir: (p) => navigationRef.navigate('Visitantes', { propriedadeId: p.id, nomePropriedade: p.nome }) },
  { id: 'vagas', rotulo: 'Vagas de garagem', icone: Car, grupo: 'Casa', visivel: soCondominio, abrir: (p) => navigationRef.navigate('Vagas', { propriedadeId: p.id, nomePropriedade: p.nome }) },
  { id: 'entregas', rotulo: 'Entregas', icone: Package, grupo: 'Casa', visivel: soCondominio, abrir: (p) => navigationRef.navigate('Entregas', { propriedadeId: p.id, nomePropriedade: p.nome }) },
  { id: 'propriedade', rotulo: 'Minha propriedade', icone: Home, grupo: 'Casa', abrir: () => navigationRef.navigate('MinhaPropriedade') },
  { id: 'pessoas', rotulo: 'Pessoas com acesso', icone: Users, grupo: 'Casa', visivel: (p) => podeGerenciarPessoas(p.perfil), abrir: () => navigationRef.navigate('Pessoas') },
  { id: 'conta', rotulo: 'Minha conta', icone: CircleUserRound, grupo: 'Conta', abrir: () => navigationRef.navigate('MinhaConta') },
  { id: 'seguranca', rotulo: 'Segurança e PIN', icone: KeyRound, grupo: 'Conta', abrir: () => navigationRef.navigate('Seguranca') },
  { id: 'emergencia', rotulo: 'Alertas e emergência', icone: ShieldAlert, grupo: 'Conta', abrir: () => navigationRef.navigate('ContatosEmergencia') },
  { id: 'notificacoes', rotulo: 'Notificações', icone: Bell, grupo: 'Conta', abrir: () => navigationRef.navigate('Notificacoes') },
  { id: 'ajustes', rotulo: 'Ajustes e conta', icone: Settings, grupo: 'Conta', abrir: () => navigationRef.navigate('Ajustes') },
  { id: 'privacidade', rotulo: 'Privacidade e termos', icone: FileText, grupo: 'Conta', abrir: () => navigationRef.navigate('Privacidade') },
  { id: 'trocar', rotulo: 'Trocar de propriedade', icone: Repeat, grupo: 'Conta', abrir: (_p, a) => a.trocarPropriedade() },
];
