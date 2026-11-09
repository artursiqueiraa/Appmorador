import { useImagemAutenticada } from '../api/useImagemAutenticada';

/** Akteck Seg (ADR 0036) — foto de rosto salva no servidor. `versao` muda quando a foto é trocada. */
export function useFotoMorador(moradorId: string, temFoto: boolean, versao = 0): string | null {
  return useImagemAutenticada(temFoto ? `/api/moradores/${moradorId}/foto` : null, versao);
}
