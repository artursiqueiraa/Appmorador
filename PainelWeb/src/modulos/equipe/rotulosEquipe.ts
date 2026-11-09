import type { RoleSistema } from '../../types/api';

export const ROTULO_PAPEL: Record<RoleSistema, string> = {
  Master: 'Master',
  Suporte: 'Suporte',
  Tecnico: 'Técnico',
};

/** Mesmo recorte de acesso das policies do backend (ADR 0021/0031). */
export const DESCRICAO_PAPEL: Record<RoleSistema, string> = {
  Master: 'Acesso total, incluindo equipe interna e novas instalações.',
  Suporte: 'Atende clientes: vê clientes, acessa a conta deles e consulta registros.',
  Tecnico: 'Instala e mantém equipamentos, provisionamentos e diagnóstico.',
};
