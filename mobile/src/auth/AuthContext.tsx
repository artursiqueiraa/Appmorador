import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { api, registerSessionExpiredHandler } from '../api/client';
import type { EntrarResponse, PropriedadeResponse } from '../api/types';
import { secureStorage, type StoredUser } from './secureStorage';
import { obterPerfil, salvarPerfil, type Perfil } from './profilePreference';

interface AuthContextValue {
  isLoading: boolean;
  user: StoredUser | null;
  selectedProperty: PropriedadeResponse | null;
  /** Sprint 17 (ADR 0020) — preferência local de UI, nunca uma fronteira de segurança real (ver `profilePreference.ts`). */
  perfil: Perfil;
  setPerfil: (perfil: Perfil) => void;
  login: (email: string, password: string) => Promise<void>;
  register: (nome: string, email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  selectProperty: (property: PropriedadeResponse) => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * Sprint 19 (ADR 0023) — mesmo padrão de `registerSessionExpiredHandler` (`api/client.ts`):
 * `PushNotificationProvider` precisa desregistrar o dispositivo (DELETE autenticado)
 * ANTES da sessão ser limpa — se esperasse `user` virar `null` para reagir, o token de
 * acesso já teria sido apagado e a chamada falharia com 401.
 */
let onBeforeLogout: (() => Promise<void>) | null = null;

export function registerBeforeLogoutHook(hook: (() => Promise<void>) | null): void {
  onBeforeLogout = hook;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<StoredUser | null>(null);
  const [selectedProperty, setSelectedProperty] = useState<PropriedadeResponse | null>(null);
  const [perfil, setPerfilState] = useState<Perfil>('morador');

  useEffect(() => {
    (async () => {
      const accessToken = await secureStorage.getAccessToken();
      setUser(accessToken ? await secureStorage.getUser() : null);
      setPerfilState(await obterPerfil());
      setIsLoading(false);
    })();
  }, []);

  // Sessão expirada de verdade (refresh token também inválido, ver client.ts) — sem
  // isso, o app continuaria "logado" em memória numa tela protegida mesmo com a
  // sessão local já limpa, falhando 401 silenciosamente em toda chamada seguinte.
  useEffect(() => {
    registerSessionExpiredHandler(() => {
      setUser(null);
      setSelectedProperty(null);
    });
  }, []);

  const login = async (email: string, password: string) => {
    const result = await api.post<EntrarResponse>('/api/auth/login', { email, senha: password });
    await secureStorage.setTokens(result.accessToken, result.refreshToken);
    const storedUser: StoredUser = { id: result.usuarioId, nome: result.nome, email: result.email };
    await secureStorage.setUser(storedUser);
    setUser(storedUser);
  };

  const register = async (nome: string, email: string, password: string) => {
    await api.post('/api/auth/register', { nome, email, senha: password });
  };

  // Sprint 18.1 (hotfix) — a limpeza local (linhas do `finally`) sempre roda, não
  // importa o que aconteça na revogação do servidor: antes, se `secureStorage.clear()`
  // por algum motivo lançasse, `setUser(null)` nunca era chamado e o usuário ficava
  // "preso" na conta. Combinado com o timeout de 15s do `client.ts`, o logout agora
  // sempre termina num tempo limitado, mesmo com o backend inalcançável.
  const logout = async () => {
    try {
      await onBeforeLogout?.().catch(() => {});
      const refreshToken = await secureStorage.getRefreshToken();
      if (refreshToken) {
        await api.post('/api/auth/logout', { refreshToken }).catch(() => {
          // Mesmo se a revogação no servidor falhar/expirar, a sessão local é limpa de qualquer forma.
        });
      }
    } finally {
      await secureStorage.clear().catch(() => {});
      setUser(null);
      setSelectedProperty(null);
    }
  };

  const selectProperty = (property: PropriedadeResponse) => setSelectedProperty(property);

  const setPerfil = (novoPerfil: Perfil) => {
    setPerfilState(novoPerfil);
    salvarPerfil(novoPerfil);
  };

  const value = useMemo(
    () => ({ isLoading, user, selectedProperty, perfil, setPerfil, login, register, logout, selectProperty }),
    [isLoading, user, selectedProperty, perfil],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth precisa ser usado dentro de um AuthProvider.');
  }

  return context;
}
