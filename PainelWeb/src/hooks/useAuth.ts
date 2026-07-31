import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';
import { authService } from '../services/authService';
import type { RoleSistema } from '../types/api';

/**
 * Sprint 22A — única fonte de verdade no Painel Web para "quem está logado" e "qual o papel
 * global". `RoleGlobal` vem sempre do JWT decodificado (nunca do body de login) — ver
 * `types/auth.ts`.
 */
export function useAuth() {
  const navigate = useNavigate();
  const { accessToken, refreshToken, user, impersonation, isLoading, setSession, clearSession, getDecoded } =
    useAuthStore();

  const decoded = getDecoded();
  const roleGlobal: RoleSistema | null = decoded?.role ?? null;
  const isAuthenticated = Boolean(accessToken && user);
  const isImpersonating = Boolean(impersonation);

  const login = useCallback(
    async (email: string, senha: string) => {
      const resposta = await authService.login({ email, senha });
      setSession(resposta.accessToken, resposta.refreshToken, {
        id: resposta.usuarioId,
        nome: resposta.nome,
        email: resposta.email,
      });
    },
    [setSession],
  );

  const logout = useCallback(async () => {
    if (refreshToken) {
      await authService.logout(refreshToken).catch(() => {
        // Mesmo se a revogação no servidor falhar, a sessão local é limpa de qualquer forma.
      });
    }
    clearSession();
    navigate('/login', { replace: true });
  }, [refreshToken, clearSession, navigate]);

  return {
    user,
    roleGlobal,
    isAuthenticated,
    isImpersonating,
    impersonation,
    isLoading,
    login,
    logout,
  };
}
