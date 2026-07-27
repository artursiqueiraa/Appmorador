import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from '../auth/AuthContext';
import { secureStorage } from '../auth/secureStorage';
import { env } from '../config/env';
import { registrarTelemetria } from '../services/telemetria';
import type { EventoResponse, SnapshotOperacionalResponse, StatusCamera } from '../api/types';

export type EstadoConexaoRealtime = 'desconectado' | 'conectando' | 'conectado' | 'reconectando' | 'sem-comunicacao';

export interface SnapshotAtualizadoEvento {
  propriedadeId: string;
  snapshot: SnapshotOperacionalResponse;
  motivo: string;
  recebidoEm: number;
}

export interface NovoEventoOperacionalEvento {
  propriedadeId: string;
  evento: EventoResponse;
  recebidoEm: number;
}

/** Sprint 20 (ADR 0024) — evento leve, separado do Snapshot Operacional (câmera é exibição, não faz parte do cálculo de saúde operacional). */
export interface CameraStatusAtualizadaEvento {
  propriedadeId: string;
  cameraId: string;
  status: StatusCamera;
  ultimaImagemUrl?: string | null;
  ultimaAtualizacaoUtc?: string | null;
  recebidoEm: number;
}

interface ConexaoContextValue {
  estadoConexao: EstadoConexaoRealtime;
  reconectarManualmente: () => void;
}

interface SnapshotContextValue {
  ultimoSnapshot: SnapshotAtualizadoEvento | null;
}

interface EventoContextValue {
  ultimoEvento: NovoEventoOperacionalEvento | null;
}

interface CameraContextValue {
  ultimaAtualizacaoCamera: CameraStatusAtualizadaEvento | null;
}

/**
 * Sprint 18 (ADR 0022, Regra 5 — Atualização Parcial Explícita) — 3 contexts
 * separados em vez de 1 só: um componente que só precisa do estado da conexão
 * (ex.: `IndicadorConexaoRealtime`) nunca deveria re-renderizar quando chega um
 * snapshot novo, e vice-versa. Um Context único faria todo consumidor
 * re-renderizar em qualquer mudança (o objeto `value` muda de referência a cada
 * `setState`, mesmo em campos que aquele consumidor não usa). Sem biblioteca
 * nova (Redux/Zustand) — só separar por preocupação, dentro do que o Context
 * do próprio React já oferece.
 */
const ConexaoContext = createContext<ConexaoContextValue | undefined>(undefined);
const SnapshotContext = createContext<SnapshotContextValue | undefined>(undefined);
const EventoContext = createContext<EventoContextValue | undefined>(undefined);
const CameraContext = createContext<CameraContextValue | undefined>(undefined);

/**
 * Backoff exponencial explícito (1s/2s/5s/10s/30s, 5 tentativas). Depois da 5ª
 * tentativa sem sucesso, `nextRetryDelayInMilliseconds` devolve `null` — o
 * SignalR desiste e chama `onclose`, momento em que o estado vira
 * `sem-comunicacao` (nunca fica tentando para sempre em silêncio).
 */
const ATRASOS_RECONEXAO_MS = [1000, 2000, 5000, 10000, 30000];

/**
 * Sprint 14 (ADR 0017) — cliente SignalR único para todo o app. SignalR é
 * exclusivamente transporte: esta camada nunca decide nada, só entra no grupo da
 * Propriedade selecionada e repassa o payload já pronto (vindo do backend) para
 * quem estiver assinando via useRealtimeSnapshot()/useRealtimeEvento(). A
 * consulta via GET/refresh manual continua funcionando de forma independente —
 * esta camada é um complemento, nunca uma dependência dura (se a conexão cair,
 * as telas seguem funcionando com o botão "Atualizar" já existente desde a
 * Sprint 13).
 *
 * Sprint 18 (ADR 0022) — Regra 3 (Cache Offline): o último snapshot/evento fica em
 * memória mesmo com a conexão caída, para que Hero/Timeline/Painel continuem
 * mostrando o último dado conhecido. Regra 4 (Política de Cache): este contexto só
 * guarda 1 snapshot e 1 evento por vez (o mais recente) — quem precisa acumular
 * uma lista com limite (Timeline: 50 eventos) faz isso na própria tela, não aqui.
 */
export function RealtimeProvider({ children }: { children: React.ReactNode }) {
  const { user, selectedProperty } = useAuth();
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const propriedadeAtualRef = useRef<string | null>(null);
  const [estadoConexao, setEstadoConexao] = useState<EstadoConexaoRealtime>('desconectado');
  const [ultimoSnapshot, setUltimoSnapshot] = useState<SnapshotAtualizadoEvento | null>(null);
  const [ultimoEvento, setUltimoEvento] = useState<NovoEventoOperacionalEvento | null>(null);
  const [ultimaAtualizacaoCamera, setUltimaAtualizacaoCamera] = useState<CameraStatusAtualizadaEvento | null>(null);
  const [tentativaManual, setTentativaManual] = useState(0);

  // Conecta quando há sessão autenticada; desconecta no logout. `tentativaManual`
  // no array de dependências permite reconstruir a conexão do zero (resetando o
  // contador de retries do SignalR) quando o usuário aciona "Tentar novamente".
  useEffect(() => {
    if (!user) {
      connectionRef.current?.stop();
      connectionRef.current = null;
      propriedadeAtualRef.current = null;
      setEstadoConexao('desconectado');
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${env.apiUrl}/hubs/operacional`, {
        accessTokenFactory: async () => (await secureStorage.getAccessToken()) ?? '',
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds(retryContext) {
          const tentativa = retryContext.previousRetryCount;
          if (tentativa >= ATRASOS_RECONEXAO_MS.length) {
            return null;
          }
          const intervalo = ATRASOS_RECONEXAO_MS[tentativa];
          registrarTelemetria({ tipo: 'signalr_reconectando', tentativa: tentativa + 1, intervaloMs: intervalo });
          return intervalo;
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('OperacionalAtualizado', (payload: { propriedadeId: string; snapshot: SnapshotOperacionalResponse; motivo: string }) => {
      registrarTelemetria({ tipo: 'snapshot_recebido', propriedadeId: payload.propriedadeId, motivo: payload.motivo });
      setUltimoSnapshot({ ...payload, recebidoEm: Date.now() });
    });

    connection.on('NovoEventoOperacional', (payload: { propriedadeId: string; evento: EventoResponse }) => {
      setUltimoEvento({ ...payload, recebidoEm: Date.now() });
      registrarTelemetria({ tipo: 'evento_processado', eventoId: payload.evento.id, msDesdeRecebido: 0 });
    });

    connection.on(
      'CameraStatusAlterado',
      (payload: {
        propriedadeId: string;
        evento: { cameraId: string; status: StatusCamera; ultimaImagemUrl?: string | null; ultimaAtualizacaoUtc?: string | null };
      }) => {
        setUltimaAtualizacaoCamera({ propriedadeId: payload.propriedadeId, ...payload.evento, recebidoEm: Date.now() });
      },
    );

    connection.onreconnecting(() => setEstadoConexao('reconectando'));
    connection.onreconnected(() => {
      setEstadoConexao('conectado');
      registrarTelemetria({ tipo: 'signalr_reconectado' });
      // Reconectar pode ter perdido mensagens publicadas durante a queda (SignalR não
      // faz replay) — reentrar no grupo garante que o servidor nos veja como
      // assinantes de novo; o fallback de "Atualizar" manual em cada tela cobre
      // qualquer atualização perdida nesse intervalo.
      if (propriedadeAtualRef.current) {
        connection.invoke('EntrarNaPropriedade', propriedadeAtualRef.current).catch(() => {});
      }
    });
    connection.onclose((erro) => {
      // Sprint 18: se a conexão já estava tentando reconectar e o SignalR desistiu
      // (retryPolicy devolveu null após as 5 tentativas), o estado vira
      // "sem-comunicacao" — só uma ação manual (reconectarManualmente) tenta de novo.
      // Se nunca chegou a conectar (falha na 1ª tentativa) ou foi um stop() explícito
      // (logout), o estado é o "desconectado" de sempre.
      setEstadoConexao((atual) => {
        if (atual === 'reconectando') {
          registrarTelemetria({ tipo: 'signalr_sem_comunicacao', tentativas: ATRASOS_RECONEXAO_MS.length });
          return 'sem-comunicacao';
        }
        return 'desconectado';
      });
      registrarTelemetria({ tipo: 'signalr_desconectado', motivo: erro?.message ?? 'fechado' });
    });

    connectionRef.current = connection;
    setEstadoConexao('conectando');

    connection
      .start()
      .then(() => {
        setEstadoConexao('conectado');
        registrarTelemetria({ tipo: 'signalr_conectado', propriedadeId: selectedProperty?.id ?? null, tentativa: tentativaManual });
      })
      .catch(() => {
        registrarTelemetria({ tipo: 'signalr_sem_comunicacao', tentativas: 0 });
        setEstadoConexao('sem-comunicacao');
      });

    return () => {
      connection.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user, tentativaManual]);

  // Entra/sai do grupo da Propriedade conforme a seleção muda — nunca acumula
  // grupos de propriedades que o usuário não está mais acompanhando.
  useEffect(() => {
    const connection = connectionRef.current;
    if (!connection || estadoConexao !== 'conectado') {
      return;
    }

    const propriedadeAnterior = propriedadeAtualRef.current;
    const novaPropriedade = selectedProperty?.id ?? null;

    if (propriedadeAnterior === novaPropriedade) {
      return;
    }

    (async () => {
      if (propriedadeAnterior) {
        await connection.invoke('SairDaPropriedade', propriedadeAnterior).catch(() => {});
      }
      if (novaPropriedade) {
        await connection.invoke('EntrarNaPropriedade', novaPropriedade).catch(() => {});
      }
      propriedadeAtualRef.current = novaPropriedade;
    })();
  }, [selectedProperty, estadoConexao]);

  // Sprint 18 (Fase 8 — Troca de Propriedade): o cache (último snapshot/evento) da
  // propriedade anterior nunca deve vazar para a nova — Hero/Timeline recomeçam
  // vazios/em carregamento até o primeiro snapshot real da nova propriedade chegar
  // (via GET inicial de cada tela ou via este mesmo canal).
  const propriedadeIdParaCacheRef = useRef<string | null>(selectedProperty?.id ?? null);
  useEffect(() => {
    const novaPropriedadeId = selectedProperty?.id ?? null;
    if (propriedadeIdParaCacheRef.current === novaPropriedadeId) {
      return;
    }
    propriedadeIdParaCacheRef.current = novaPropriedadeId;
    setUltimoSnapshot(null);
    setUltimoEvento(null);
    setUltimaAtualizacaoCamera(null);
  }, [selectedProperty?.id]);

  const reconectarManualmente = useCallback(() => {
    setTentativaManual((atual) => atual + 1);
  }, []);

  const valorConexao = useMemo(() => ({ estadoConexao, reconectarManualmente }), [estadoConexao, reconectarManualmente]);
  const valorSnapshot = useMemo(() => ({ ultimoSnapshot }), [ultimoSnapshot]);
  const valorEvento = useMemo(() => ({ ultimoEvento }), [ultimoEvento]);
  const valorCamera = useMemo(() => ({ ultimaAtualizacaoCamera }), [ultimaAtualizacaoCamera]);

  return (
    <ConexaoContext.Provider value={valorConexao}>
      <SnapshotContext.Provider value={valorSnapshot}>
        <EventoContext.Provider value={valorEvento}>
          <CameraContext.Provider value={valorCamera}>{children}</CameraContext.Provider>
        </EventoContext.Provider>
      </SnapshotContext.Provider>
    </ConexaoContext.Provider>
  );
}

export function useRealtimeConexao(): ConexaoContextValue {
  const context = useContext(ConexaoContext);
  if (!context) {
    throw new Error('useRealtimeConexao precisa ser usado dentro de um RealtimeProvider.');
  }
  return context;
}

export function useRealtimeSnapshot(): SnapshotContextValue {
  const context = useContext(SnapshotContext);
  if (!context) {
    throw new Error('useRealtimeSnapshot precisa ser usado dentro de um RealtimeProvider.');
  }
  return context;
}

export function useRealtimeEvento(): EventoContextValue {
  const context = useContext(EventoContext);
  if (!context) {
    throw new Error('useRealtimeEvento precisa ser usado dentro de um RealtimeProvider.');
  }
  return context;
}

export function useRealtimeCamera(): CameraContextValue {
  const context = useContext(CameraContext);
  if (!context) {
    throw new Error('useRealtimeCamera precisa ser usado dentro de um RealtimeProvider.');
  }
  return context;
}
