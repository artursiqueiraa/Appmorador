import { useAuth } from './useAuth';
import type { RoleSistema } from '../types/api';

/** Sprint 22A — booleans de conveniência por papel global, mesmo espírito do `usePermissao` do app mobile. */
export function usePermissao() {
  const { roleGlobal } = useAuth();

  const isMaster = roleGlobal === 'Master';
  const isTecnico = roleGlobal === 'Tecnico';
  const isSuporte = roleGlobal === 'Suporte';
  const temAlgumRole = (...roles: RoleSistema[]) => roleGlobal !== null && roles.includes(roleGlobal);

  return {
    roleGlobal,
    isMaster,
    isTecnico,
    isSuporte,
    /** Master ∪ Suporte — quem tem impersonation/visão global (ver ADR 0021). */
    podeImpersonar: isMaster || isSuporte,
    /** Master ∪ Suporte — quem vê auditoria/clientes/dashboard. */
    podeVerTudo: isMaster || isSuporte,
    /** Master ∪ Técnico — mesmo predicado de `Policies.RequerTecnico` no backend (ADR 0031, Sprint 22B). */
    podeGerenciarHardware: isMaster || isTecnico,
    temAlgumRole,
  };
}
