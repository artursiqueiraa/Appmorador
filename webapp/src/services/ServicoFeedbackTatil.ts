import * as Haptics from 'expo-haptics';

/**
 * Único ponto de contato com `expo-haptics` no app — nenhuma tela chama a lib
 * diretamente. Padroniza o vocabulário de feedback tátil por semântica de ação, não
 * por API nativa.
 *
 * Web App — a maioria dos navegadores (e todo desktop) não tem motor de vibração;
 * a falha é sempre silenciosa, o feedback visual da tela continua sendo o principal.
 */
const silenciar = (acao: () => Promise<void>) => () => acao().catch(() => {});

export const ServicoFeedbackTatil = {
  impactLight: silenciar(() => Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light)),
  impactMedium: silenciar(() => Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium)),
  notificationError: silenciar(() => Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error)),
};
