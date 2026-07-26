import React, { createContext, useContext, useEffect, useMemo, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from '../auth/AuthContext';
import { secureStorage } from '../auth/secureStorage';
import { env } from '../config/env';
import type { EventoResponse, SnapshotOperacionalResponse } from '../api/types';

export type EstadoConexaoRealtime = 'desconectado' | 'conectando' | 'conectado' | 'reconectando';

interface SnapshotAtualizadoEvento {
  propriedadeId: string;
  snapshot: SnapshotOperacionalResponse;
  motivo: string;
  recebidoEm: number;
}

interface NovoEventoOperacionalEvento {
  propriedadeId: string;
  evento: EventoResponse;
  recebidoEm: number;
}

interface RealtimeContextValue {
  estadoConexao: EstadoConexaoRealtime;
  ultimoSnapshot: SnapshotAtualizadoEvento | null;
  ultimoEvento: NovoEventoOperacionalEvento | null;
}

const RealtimeContext = createContext<RealtimeContextValue | undefined>(undefined);

/**
 * Sprint 14 (ADR 0017) — cliente SignalR único para todo o app. SignalR é
 * exclusivamente transporte: esta camada nunca decide nada, só entra no grupo da
 * Propriedade selecionada e repassa o payload já pronto (vindo do backend) para
 * quem estiver assinando via useRealtime(). A consulta via GET/refresh manual
 * continua funcionando de forma independente — esta camada é um complemento, nunca
 * uma dependência dura (se a conexão cair, as telas seguem funcionando com o botão
 * "Atualizar" já existente desde a Sprint 13).
 */
export function RealtimeProvider({ children }: { children: React.ReactNode }) {
  const { user, selectedProperty } = useAuth();
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const propriedadeAtualRef = useRef<string | null>(null);
  const [estadoConexao, setEstadoConexao] = useState<EstadoConexaoRealtime>('desconectado');
  const [ultimoSnapshot, setUltimoSnapshot] = useState<SnapshotAtualizadoEvento | null>(null);
  const [ultimoEvento, setUltimoEvento] = useState<NovoEventoOperacionalEvento | null>(null);

  // Conecta quando há sessão autenticada; desconecta no logout.
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
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('OperacionalAtualizado', (payload: { propriedadeId: string; snapshot: SnapshotOperacionalResponse; motivo: string }) => {
      setUltimoSnapshot({ ...payload, recebidoEm: Date.now() });
    });

    connection.on('NovoEventoOperacional', (payload: { propriedadeId: string; evento: EventoResponse }) => {
      setUltimoEvento({ ...payload, recebidoEm: Date.now() });
    });

    connection.onreconnecting(() => setEstadoConexao('reconectando'));
    connection.onreconnected(() => {
      setEstadoConexao('conectado');
      // Reconectar pode ter perdido mensagens publicadas durante a queda (SignalR não
      // faz replay) — reentrar no grupo garante que o servidor nos veja como
      // assinantes de novo; o fallback de "Atualizar" manual em cada tela cobre
      // qualquer atualização perdida nesse intervalo.
      if (propriedadeAtualRef.current) {
        connection.invoke('EntrarNaPropriedade', propriedadeAtualRef.current).catch(() => {});
      }
    });
    connection.onclose(() => setEstadoConexao('desconectado'));

    connectionRef.current = connection;
    setEstadoConexao('conectando');

    connection
      .start()
      .then(() => setEstadoConexao('conectado'))
      .catch(() => setEstadoConexao('desconectado'));

    return () => {
      connection.stop();
    };
  }, [user]);

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

  const value = useMemo(
    () => ({ estadoConexao, ultimoSnapshot, ultimoEvento }),
    [estadoConexao, ultimoSnapshot, ultimoEvento],
  );

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}

export function useRealtime(): RealtimeContextValue {
  const context = useContext(RealtimeContext);
  if (!context) {
    throw new Error('useRealtime precisa ser usado dentro de um RealtimeProvider.');
  }

  return context;
}
