import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { api, registerSessionExpiredHandler, restaurarSessao } from "../api/client";
import type { EntrarResponse, PropriedadeResponse } from "../api/types";
import { ehWeb, secureStorage, type StoredUser } from "./secureStorage";
import { obterPerfil, salvarPerfil, type Perfil } from "./profilePreference";
import { invalidarSituacaoPin } from "../seguranca/confirmarComando";

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
  /** Akteck Seg (ADR 0037) — volta para "Suas propriedades" sem sair da conta (seletor do Início). */
  trocarPropriedade: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * Sprint 19 (ADR 0023) — mesmo padrão de `registerSessionExpiredHandler` (`api/client.ts`):
 * `PushNotificationProvider` precisa desregistrar o dispositivo (DELETE autenticado)
 * ANTES da sessão ser limpa — se esperasse `user` virar `null` para reagir, o token de
 * acesso já teria sido apagado e a chamada falharia com 401.
 */
let onBeforeLogout: (() => Promise<void>) | null = null;

export function registerBeforeLogoutHook(
  hook: (() => Promise<void>) | null,
): void {
  onBeforeLogout = hook;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<StoredUser | null>(null);
  const [selectedProperty, setSelectedProperty] =
    useState<PropriedadeResponse | null>(null);
  // Sprint 25 (ADR 0041, dívida 34) — a preferência local só vale para quem o papel permite
  // (Proprietário/Gerente); Membro e Visualizador sempre veem o modo morador.
  const [preferenciaPerfil, setPerfilState] = useState<Perfil>("morador");
  const papelPermiteTecnico =
    selectedProperty?.perfil === "Administrador" ||
    selectedProperty?.perfil === "Gerente";
  const perfil: Perfil = papelPermiteTecnico ? preferenciaPerfil : "morador";

  useEffect(() => {
    (async () => {
      // Sprint 27 — no web, o access token só vive em memória: renova pelo cookie HttpOnly.
      const usuarioGuardado = await secureStorage.getUser();
      const sessaoValida =
        usuarioGuardado !== null && (await restaurarSessao().catch(() => false));
      setUser(sessaoValida ? usuarioGuardado : null);
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
    const result = await api.post<EntrarResponse>("/api/auth/login", {
      email,
      senha: password,
    });
    await secureStorage.setTokens(result.accessToken, result.refreshToken);
    const storedUser: StoredUser = {
      id: result.usuarioId,
      nome: result.nome,
      email: result.email,
    };
    await secureStorage.setUser(storedUser);
    invalidarSituacaoPin();
    setUser(storedUser);
  };

  const register = async (nome: string, email: string, password: string) => {
    await api.post("/api/auth/register", { nome, email, senha: password });
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
      // No web o refresh token está no cookie HttpOnly: a Api o lê e revoga.
      if (refreshToken || ehWeb) {
        await api.post("/api/auth/logout", { refreshToken: refreshToken ?? "" }).catch(() => {
          // Mesmo se a revogação no servidor falhar/expirar, a sessão local é limpa de qualquer forma.
        });
      }
    } finally {
      await secureStorage.clear().catch(() => {});
      invalidarSituacaoPin();
      setUser(null);
      setSelectedProperty(null);
    }
  };

  const selectProperty = (property: PropriedadeResponse) =>
    setSelectedProperty(property);
  const trocarPropriedade = () => setSelectedProperty(null);

  const setPerfil = (novoPerfil: Perfil) => {
    setPerfilState(novoPerfil);
    salvarPerfil(novoPerfil);
  };

  const value = useMemo(
    () => ({
      isLoading,
      user,
      selectedProperty,
      perfil,
      setPerfil,
      login,
      register,
      logout,
      selectProperty,
      trocarPropriedade,
    }),
    [isLoading, user, selectedProperty, perfil],
    // perfil já deriva de selectedProperty + preferenciaPerfil
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth precisa ser usado dentro de um AuthProvider.");
  }

  return context;
}
