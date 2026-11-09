import { useEffect, useRef } from 'react';
import { Platform } from 'react-native';
import * as Notifications from 'expo-notifications';
import { registerBeforeLogoutHook, useAuth } from '../auth/AuthContext';
import { navigationRef } from '../navigation/navigationRef';
import { abrirAlertaPorNotificacao } from '../alertas/abrirAlertaPorNotificacao';
import { configurarCanaisAndroidAsync } from './pushChannels';
import { atualizarTokenAsync, desregistrarAsync, reenviarRegistroSeJaPermitidoAsync, solicitarPermissaoERegistrarAsync } from './pushService';

/**
 * Sprint 19 (ADR 0023, Fase 5) — enquanto o app está em primeiro plano, o
 * SignalR (Sprint 18) já atualiza a tela e o `RealtimeToastBridge` já mostra um
 * toast discreto quando o evento acontece fora da tela em foco — mostrar TAMBÉM
 * a notificação do sistema aqui seria duplicar o aviso. `handleNotification` só
 * roda com o app em primeiro plano (é a definição do hook), então suprimir
 * incondicionalmente aqui está correto: com o app fechado/em segundo plano, quem
 * decide exibir é o próprio SO, não este código.
 */
Notifications.setNotificationHandler({
  handleNotification: async (notificacao) => {
    // Sprint 24 (ADR 0040) — alerta crítico com o app aberto: abre a tela cheia e toca o som
    // mesmo que o tempo real esteja desconectado (o push é o caminho garantido).
    const dados = notificacao.request.content.data;
    const critico = dados?.acao === 'ABRIR_ALERTA';
    // `local`: aviso sonoro gerado pela própria tela de alerta (já está aberta, não reabrir).
    if (critico && !dados?.local) {
      abrirAlertaPorNotificacao(dados?.alertaId);
    }
    return { shouldShowBanner: false, shouldShowList: critico, shouldPlaySound: critico, shouldSetBadge: false };
  },
});

/** Sprint 19 (ADR 0023, Fase 5) — mapeia a `acao` que o backend manda no payload (ver NotificationDispatcher) para onde o Mobile navega ao tocar na notificação. */
const ACAO_PARA_TELA: Record<string, { rota: 'Eventos' } | { rota: 'MainTabs'; aba: 'Inicio' | 'Acessos' }> = {
  ABRIR_APP_HISTORICO: { rota: 'Eventos' },
  ABRIR_APP_INICIO: { rota: 'MainTabs', aba: 'Inicio' },
  ABRIR_APP_ACESSOS: { rota: 'MainTabs', aba: 'Acessos' },
};

/**
 * Sprint 19 — quando o app é aberto a frio por um toque na notificação, o
 * `NavigationContainer` (dentro de `RootNavigator`, montado por um componente
 * ACIMA deste) ainda não existe no primeiro instante. Tenta por até 3s (10
 * tentativas de 300ms) antes de desistir silenciosamente — nunca trava nem
 * lança, só não navega (o app já abriu normalmente, no pior caso na tela inicial).
 */
export function navegarParaAcao(acao: unknown, tentativasRestantes = 10, alertaId?: unknown): void {
  if (typeof acao !== 'string') {
    return;
  }
  if (acao === 'ABRIR_ALERTA') {
    abrirAlertaPorNotificacao(alertaId);
    return;
  }
  const destino = ACAO_PARA_TELA[acao];
  if (!destino) {
    return;
  }

  if (!navigationRef.isReady()) {
    if (tentativasRestantes > 0) {
      setTimeout(() => navegarParaAcao(acao, tentativasRestantes - 1), 300);
    }
    return;
  }

  if (destino.rota === 'MainTabs') {
    navigationRef.navigate('MainTabs', { screen: destino.aba });
  } else {
    navigationRef.navigate('Eventos');
  }
}

/**
 * Sprint 19 (ADR 0023) — complemento ao `RealtimeProvider` (SignalR): aquele
 * cobre "app aberto", este cobre "app fechado/em segundo plano". Sem UI própria
 * (mesmo padrão do `RealtimeToastBridge`) — só orquestra permissão, ciclo de vida
 * do token e deep link.
 */
function PushNotificationProviderNativo({ children }: { children: React.ReactNode }) {
  const { user, selectedProperty } = useAuth();
  const usuarioLogadoAnteriormenteRef = useRef(false);
  const propriedadeRegistradaRef = useRef<string | null>(null);

  useEffect(() => {
    configurarCanaisAndroidAsync().catch(() => {});
  }, []);

  useEffect(() => {
    registerBeforeLogoutHook(() => desregistrarAsync());
    return () => registerBeforeLogoutHook(null);
  }, []);

  // Fase 7.1 — solicita permissão só na transição "sem sessão" -> "com sessão"
  // (primeiro login, ou reabertura do app com sessão salva), nunca a cada
  // renderização nem a cada troca de propriedade.
  useEffect(() => {
    const estaLogado = !!user;
    if (estaLogado && !usuarioLogadoAnteriormenteRef.current) {
      solicitarPermissaoERegistrarAsync(selectedProperty?.id ?? null).catch(() => {});
      propriedadeRegistradaRef.current = selectedProperty?.id ?? null;
    }
    usuarioLogadoAnteriormenteRef.current = estaLogado;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user]);

  // Propriedade selecionada mudou depois do registro inicial — reenvia só o hint
  // (PropriedadeId), sem exibir nenhum diálogo de permissão de novo.
  useEffect(() => {
    if (!user) {
      return;
    }
    const novaPropriedadeId = selectedProperty?.id ?? null;
    if (propriedadeRegistradaRef.current === novaPropriedadeId) {
      return;
    }
    propriedadeRegistradaRef.current = novaPropriedadeId;
    reenviarRegistroSeJaPermitidoAsync(novaPropriedadeId).catch(() => {});
  }, [user, selectedProperty?.id]);

  useEffect(() => {
    const subscription = Notifications.addPushTokenListener((token) => {
      atualizarTokenAsync(String(token.data)).catch(() => {});
    });
    return () => subscription.remove();
  }, []);

  useEffect(() => {
    const respostaAoAbrir = Notifications.getLastNotificationResponse();
    if (respostaAoAbrir) {
      const dados = respostaAoAbrir.notification.request.content.data;
      navegarParaAcao(dados?.acao, undefined, dados?.alertaId);
    }

    const subscription = Notifications.addNotificationResponseReceivedListener((resposta) => {
      const dados = resposta.notification.request.content.data;
      navegarParaAcao(dados?.acao, undefined, dados?.alertaId);
    });
    return () => subscription.remove();
  }, []);

  return children;
}

/**
 * Web App — no navegador não existe push (ver `PUSH_DISPONIVEL` em `pushService.ts`)
 * e as APIs nativas de `expo-notifications` lançam `UnavailabilityError`. Na web o
 * provider só repassa os filhos; os avisos com a página aberta seguem pelo SignalR.
 */
export const PushNotificationProvider =
  Platform.OS === 'web' ? ({ children }: { children: React.ReactNode }) => children : PushNotificationProviderNativo;
