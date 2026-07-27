import { useAuth } from './AuthContext';
import type { FeatureFlag, PermissaoFuncionalidade } from '../api/types';

/**
 * Sprint 21 (ADR 0021/0025/0026) — única fonte de verdade no app para "o que este
 * usuário pode fazer" (permissoes) e "o que esta propriedade contratou" (features).
 * Lê de `selectedProperty` (populado por GET /api/properties, já enriquecido pelo
 * backend) — nunca do `perfil` local de `profilePreference.ts` (que é só uma
 * preferência de UI sem relação com este modelo, ver ADR 0020). Sem propriedade
 * selecionada, tudo é negado por padrão (fail-closed) em vez de lançar.
 */
export function usePermissao() {
  const { selectedProperty } = useAuth();

  const perfil = selectedProperty?.perfil ?? null;
  const permissoes = selectedProperty?.permissoes ?? [];
  const features = selectedProperty?.features ?? [];

  const temPermissao = (permissao: PermissaoFuncionalidade): boolean => permissoes.includes(permissao);
  const temFeature = (feature: FeatureFlag): boolean => features.includes(feature);

  return { perfil, permissoes, features, temPermissao, temFeature };
}
