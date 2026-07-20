import * as Haptics from 'expo-haptics';

/**
 * Único ponto de contato com `expo-haptics` no app — nenhuma tela chama a lib
 * diretamente. Padroniza o vocabulário de feedback tátil por semântica de ação, não
 * por API nativa.
 */
export const ServicoFeedbackTatil = {
  impactLight: () => Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light),
  impactMedium: () => Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium),
  notificationError: () => Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error),
};
