import { useEffect, useRef } from 'react';
import * as Notifications from 'expo-notifications';
import { registerBeforeLogoutHook, useAuth } from '../auth/AuthContext';
import { navigationRef } from '../navigation/navigationRef';
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
  handleNotification: async () => ({
    shouldShowBanner: false,
    shouldShowList: false,
    shouldPlaySound: false,
    shouldSetBadge: false,
  }),
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
export function navegarParaAcao(acao: unknown, tentativasRestantes = 10): void {
  if (typeof acao !== 'string') {
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
export function PushNotificationProvider({ children }: { children: React.ReactNode }) {
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
      navegarParaAcao(respostaAoAbrir.notification.request.content.data?.acao);
    }

    const subscription = Notifications.addNotificationResponseReceivedListener((resposta) => {
      navegarParaAcao(resposta.notification.request.content.data?.acao);
    });
    return () => subscription.remove();
  }, []);

  return children;
}
