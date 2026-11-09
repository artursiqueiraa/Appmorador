import type { RoleSistema } from '../../../types/api';

/** Espelha `UsuarioInternoResponse` (backend, `AppMorador.Application.Rbac`). */
export interface MembroEquipe {
  id: string;
  nome: string;
  email: string;
  roleGlobal: RoleSistema;
  ativo: boolean;
  createdAtUtc: string;
}

/** Espelha `CriarUsuarioInternoRequest`. */
export interface NovoMembroEquipe {
  nome: string;
  email: string;
  senha: string;
  roleGlobal: RoleSistema;
}
