import type { RoleSistema } from './api';

/**
 * Sprint 22A — shape real das claims do JWT emitido por `JwtTokenService` (ver
 * ARQUITETURA_ATUAL.md). `nome` NUNCA está aqui — só vem no corpo de
 * `EntrarResponse`/`ImpersonarResponse`, precisa ser persistido à parte.
 */
export interface DecodedToken {
  sub: string;
  email: string;
  securityStamp: string;
  jti: string;
  role?: RoleSistema;
  impersonating?: 'true';
  impersonatedBy?: string;
  impersonatedByNome?: string;
  exp: number;
  iss: string;
  aud: string;
}

export interface StoredUser {
  id: string;
  nome: string;
  email: string;
}

export interface ImpersonationState {
  propriedadeId: string;
  propriedadeNome: string;
  clienteNome: string;
  /** Token do Master/Suporte, guardado para restaurar ao encerrar. */
  tokenOriginal: string;
  expiresAtUtc: string;
}
