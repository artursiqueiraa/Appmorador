import { useEffect, useState } from 'react';
import { httpClient } from '../services/httpClient';

/**
 * Sprint 22A — `GET /api/cameras/{id}/imagem` exige Bearer (nunca static files, ver ADR 0024) —
 * um `<img src>` puro não consegue mandar o header. Busca como blob e expõe uma Object URL,
 * mesmo padrão do `useAuthHeader` do app mobile, adaptado para web.
 */
export function useAuthenticatedImage(caminhoRelativo: string | null | undefined): string | null {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!caminhoRelativo) return;

    let objectUrl: string | null = null;
    let cancelado = false;

    httpClient
      .get(caminhoRelativo, { responseType: 'blob' })
      .then((resposta) => {
        if (cancelado) return;
        objectUrl = URL.createObjectURL(resposta.data as Blob);
        setUrl(objectUrl);
      })
      .catch(() => setUrl(null));

    return () => {
      cancelado = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [caminhoRelativo]);

  return caminhoRelativo ? url : null;
}
