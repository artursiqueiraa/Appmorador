import { useEffect, useState } from 'react';
import { obterBlobAutenticado } from './client';

/**
 * Akteck Seg (ADR 0036/0037) — URL local (blob:) de uma imagem da Api que exige token (foto do
 * morador, última imagem da câmera). A Api nunca serve essas imagens como arquivo público, e
 * `<Image>` no navegador não envia header de autenticação. `chave` muda para forçar recarga.
 * A URL é revogada ao desmontar.
 */
export function useImagemAutenticada(caminho: string | null | undefined, chave: unknown = 0): string | null {
  const [uri, setUri] = useState<string | null>(null);

  useEffect(() => {
    if (!caminho) {
      setUri(null);
      return;
    }
    let ativo = true;
    let criada: string | null = null;
    obterBlobAutenticado(caminho).then((blob) => {
      if (!ativo || !blob) return;
      criada = URL.createObjectURL(blob);
      setUri(criada);
    });
    return () => {
      ativo = false;
      // Revoga depois da troca de tela: a <img> antiga ainda pode estar no DOM por um instante.
      const antiga = criada;
      if (antiga) setTimeout(() => URL.revokeObjectURL(antiga), 1000);
    };
  }, [caminho, chave]);

  return uri;
}
