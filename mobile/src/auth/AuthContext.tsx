import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { api } from '../api/client';
import type { EntrarResponse, PropriedadeResponse } from '../api/types';
import { secureStorage, type StoredUser } from './secureStorage';

interface AuthContextValue {
  isLoading: boolean;
  user: StoredUser | null;
  selectedProperty: PropriedadeResponse | null;
  login: (email: string, password: string) => Promise<void>;
  register: (nome: string, email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  selectProperty: (property: PropriedadeResponse) => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<StoredUser | null>(null);
  const [selectedProperty, setSelectedProperty] = useState<PropriedadeResponse | null>(null);

  useEffect(() => {
    (async () => {
      const accessToken = await secureStorage.getAccessToken();
      setUser(accessToken ? await secureStorage.getUser() : null);
      setIsLoading(false);
    })();
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

  const logout = async () => {
    const refreshToken = await secureStorage.getRefreshToken();
    if (refreshToken) {
      try {
        await api.post('/api/auth/logout', { refreshToken });
      } catch {
        // Mesmo se a revogação no servidor falhar, a sessão local é limpa de qualquer forma.
      }
    }

    await secureStorage.clear();
    setUser(null);
    setSelectedProperty(null);
  };

  const selectProperty = (property: PropriedadeResponse) => setSelectedProperty(property);

  const value = useMemo(
    () => ({ isLoading, user, selectedProperty, login, register, logout, selectProperty }),
    [isLoading, user, selectedProperty],
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
