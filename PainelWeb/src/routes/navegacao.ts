import type { SvgIconComponent } from '@mui/icons-material';
import SpaceDashboardOutlinedIcon from '@mui/icons-material/SpaceDashboardOutlined';
import PeopleAltOutlinedIcon from '@mui/icons-material/PeopleAltOutlined';
import AddHomeWorkOutlinedIcon from '@mui/icons-material/AddHomeWorkOutlined';
import DevicesOtherOutlinedIcon from '@mui/icons-material/DevicesOtherOutlined';
import LinkOutlinedIcon from '@mui/icons-material/LinkOutlined';
import MonitorHeartOutlinedIcon from '@mui/icons-material/MonitorHeartOutlined';
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined';
import SupportAgentOutlinedIcon from '@mui/icons-material/SupportAgentOutlined';
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined';
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined';
import BadgeOutlinedIcon from '@mui/icons-material/BadgeOutlined';
import type { usePermissao } from '../hooks/usePermissao';

type Permissoes = ReturnType<typeof usePermissao>;

export interface ItemNavegacao {
  rotulo: string;
  rota: string;
  icone: SvgIconComponent;
  visivel: (p: Permissoes) => boolean;
}

export interface GrupoNavegacao {
  rotulo: string;
  itens: ItemNavegacao[];
}

/**
 * Akteck Seg (ADR 0035) — fonte única da navegação: o menu lateral desenha os grupos,
 * e o cabeçalho usa a mesma lista para o título e a trilha "Grupo / Página". Antes,
 * o menu era uma lista plana e o cabeçalho não sabia em que tela o usuário estava.
 */
export const GRUPOS_NAVEGACAO: GrupoNavegacao[] = [
  {
    rotulo: 'Visão geral',
    itens: [
      {
        rotulo: 'Dashboard',
        rota: '/dashboard',
        icone: SpaceDashboardOutlinedIcon,
        visivel: (p) => !(p.isTecnico && !p.isMaster && !p.isSuporte),
      },
      {
        rotulo: 'Dashboard',
        rota: '/dashboard-tecnico',
        icone: SpaceDashboardOutlinedIcon,
        visivel: (p) => p.isTecnico && !p.isMaster && !p.isSuporte,
      },
    ],
  },
  {
    rotulo: 'Clientes',
    itens: [
      { rotulo: 'Clientes', rota: '/clientes', icone: PeopleAltOutlinedIcon, visivel: (p) => p.podeVerTudo },
      { rotulo: 'Nova instalação', rota: '/nova-instalacao', icone: AddHomeWorkOutlinedIcon, visivel: (p) => p.isMaster },
    ],
  },
  {
    rotulo: 'Equipamentos',
    itens: [
      { rotulo: 'Equipamentos', rota: '/equipamentos', icone: DevicesOtherOutlinedIcon, visivel: (p) => p.podeGerenciarHardware },
      { rotulo: 'Provisionamentos', rota: '/provisionamentos', icone: LinkOutlinedIcon, visivel: (p) => p.podeGerenciarHardware },
      { rotulo: 'Diagnóstico', rota: '/diagnostico-equipamentos', icone: MonitorHeartOutlinedIcon, visivel: (p) => p.podeGerenciarHardware },
      { rotulo: 'Catálogo de modelos', rota: '/modelos-equipamento', icone: CategoryOutlinedIcon, visivel: (p) => p.podeGerenciarHardware },
    ],
  },
  {
    rotulo: 'Suporte',
    itens: [
      { rotulo: 'Acessar como cliente', rota: '/suporte/selecionar-cliente', icone: SupportAgentOutlinedIcon, visivel: (p) => p.podeVerTudo },
      { rotulo: 'Sessões ativas', rota: '/suporte/sessoes-ativas', icone: HistoryOutlinedIcon, visivel: (p) => p.podeVerTudo },
      { rotulo: 'Registro de atividades', rota: '/suporte/logs', icone: ReceiptLongOutlinedIcon, visivel: (p) => p.podeVerTudo },
    ],
  },
  {
    rotulo: 'Administração',
    itens: [{ rotulo: 'Equipe interna', rota: '/equipe-interna', icone: BadgeOutlinedIcon, visivel: (p) => p.isMaster }],
  },
];

/** Item ativo por rota exata ou sub-rota ("/clientes/123" ativa "Clientes"), nunca por prefixo de grupo. */
export function itemAtivo(rotaAtual: string, item: ItemNavegacao): boolean {
  return rotaAtual === item.rota || rotaAtual.startsWith(`${item.rota}/`);
}

export function localizarNaNavegacao(rotaAtual: string): { grupo: GrupoNavegacao; item: ItemNavegacao } | null {
  // "/suporte/diagnostico" é aberto a partir de "Acessar como cliente".
  const rota = rotaAtual.startsWith('/suporte/diagnostico') ? '/suporte/selecionar-cliente' : rotaAtual;
  for (const grupo of GRUPOS_NAVEGACAO) {
    const item = grupo.itens.find((i) => itemAtivo(rota, i));
    if (item) {
      return { grupo, item };
    }
  }
  return null;
}
