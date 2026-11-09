import { useEffect, useState } from 'react';
import { secureStorage } from '../auth/secureStorage';

/**
 * Sprint 20 (ADR 0024) — a Api serve a imagem da câmera autenticada (Bearer), nunca
 * via static files públicos. `expo-image`/`Image` não têm como anexar um header de
 * autenticação "automaticamente" como o `api` client faz para JSON — cada
 * componente que renderiza uma imagem de câmera busca o token uma vez e monta o
 * header ele mesmo, via `source={{ uri, headers }}`.
 */
export function useAuthHeader(): Record<string, string> | undefined {
  const [header, setHeader] = useState<Record<string, string> | undefined>(undefined);

  useEffect(() => {
    let montado = true;
    secureStorage.getAccessToken().then((token) => {
      if (montado && token) {
        setHeader({ Authorization: `Bearer ${token}` });
      }
    });
    return () => {
      montado = false;
    };
  }, []);

  return header;
}
