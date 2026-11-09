import { useEffect, useState } from 'react';
import { obterBlobAutenticado } from './client';

/**
 * Base única (ADR 0039) — versão Android/iOS do hook de imagem autenticada. O React Native não tem
 * `URL.createObjectURL`, então a imagem baixada com o token vira um `data:` URI, que a `<Image>`
 * nativa exibe direto. A versão do navegador (`useImagemAutenticada.web.ts`) usa `blob:` URL.
 * Mesma assinatura nas duas: `chave` muda para forçar recarga.
 */
export function useImagemAutenticada(caminho: string | null | undefined, chave: unknown = 0): string | null {
  const [uri, setUri] = useState<string | null>(null);

  useEffect(() => {
    if (!caminho) {
      setUri(null);
      return;
    }
    let ativo = true;
    obterBlobAutenticado(caminho).then((blob) => {
      if (!ativo || !blob) return;
      const leitor = new FileReader();
      leitor.onloadend = () => {
        if (ativo && typeof leitor.result === 'string') setUri(leitor.result);
      };
      leitor.readAsDataURL(blob);
    });
    return () => {
      ativo = false;
    };
  }, [caminho, chave]);

  return uri;
}
