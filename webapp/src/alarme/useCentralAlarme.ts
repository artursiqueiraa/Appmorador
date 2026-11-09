import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../api/client';
import type { EquipamentoResponse, ParticaoStatusInfo, ResultadoComandoJfl, StatusCentralJflInfo } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { ServicoFeedbackTatil } from '../services/ServicoFeedbackTatil';
import { confirmarComando, novaChave, type CabecalhosComando } from '../seguranca/confirmarComando';

/** Estado do alarme como o protótipo mostra: total (PROTEGIDO), parcial (MODO NOTURNO), desarmado, disparo. */
export type EstadoAlarme = 'total' | 'parcial' | 'desarmado' | 'disparo' | 'semComunicacao' | 'semCentral' | 'carregando';

/** Comando real da central JFL. `armar-stay` é o arme parcial ("Noturno"). */
export type ComandoAlarme = 'armar' | 'armar-stay' | 'desarmar';

function estadoDasParticoes(particoes: ParticaoStatusInfo[]): EstadoAlarme {
  const ativas = particoes.filter((p) => !p.desabilitada);
  if (ativas.length === 0) return 'semComunicacao';
  if (ativas.some((p) => p.emDisparo)) return 'disparo';
  if (ativas.some((p) => p.armada && !p.armadaStay)) return 'total';
  if (ativas.some((p) => p.armadaStay)) return 'parcial';
  return 'desarmado';
}

/**
 * Akteck Seg (ADR 0038) — estado e comandos REAIS do alarme (central JFL), usados pelo cartão
 * de status do Início e pela aba Alarme. Antes, os botões do Início só mudavam um estado local.
 * Os comandos vão para cada partição habilitada da central escolhida.
 */
export function useCentralAlarme() {
  const { selectedProperty } = useAuth();
  const [centrais, setCentrais] = useState<EquipamentoResponse[] | null>(null);
  const [centralId, setCentralId] = useState<string | null>(null);
  const [status, setStatus] = useState<StatusCentralJflInfo | null>(null);
  const [lendo, setLendo] = useState(false);
  const [enviando, setEnviando] = useState<ComandoAlarme | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [atualizadoEm, setAtualizadoEm] = useState<Date | null>(null);

  useEffect(() => {
    if (!selectedProperty) return;
    setCentrais(null);
    setStatus(null);
    api
      .get<EquipamentoResponse[]>(`/api/properties/${selectedProperty.id}/equipamentos`)
      .then((lista) => {
        const jfl = lista.filter((e) => e.fabricante === 'Jfl');
        setCentrais(jfl);
        setCentralId(jfl[0]?.id ?? null);
      })
      .catch(() => setCentrais([]));
  }, [selectedProperty]);

  const lerStatus = useCallback(async () => {
    if (!centralId) return;
    setLendo(true);
    setErro(null);
    try {
      const r = await api.get<ResultadoComandoJfl>(`/api/equipamentos/${centralId}/jfl/status`);
      if (r.sucesso && r.statusResultante) {
        setStatus(r.statusResultante);
        setAtualizadoEm(new Date());
      } else {
        setStatus(null);
        setErro(r.mensagemErro ?? 'A central não respondeu.');
      }
    } catch (e) {
      setStatus(null);
      setErro(e instanceof ApiError ? e.message : 'A central não respondeu.');
    } finally {
      setLendo(false);
    }
  }, [centralId]);

  useEffect(() => {
    setStatus(null);
    lerStatus();
  }, [lerStatus]);

  const particoes = (status?.particoes ?? []).filter((p) => !p.desabilitada);
  const central = centrais?.find((c) => c.id === centralId) ?? null;

  const comandar = async (comando: ComandoAlarme) => {
    if (!centralId || particoes.length === 0) return;
    ServicoFeedbackTatil.impactLight();
    setEnviando(comando);
    setErro(null);
    try {
      // Sprint 27 (ADR 0043) — uma chave por partição (a mesma em cada nova tentativa de PIN).
      const enviarTodas = async (cabecalhos: CabecalhosComando) => {
        let ultimoStatus: StatusCentralJflInfo | null = null;
        for (const p of particoes) {
          const r = await api.post<ResultadoComandoJfl>(
            `/api/equipamentos/${centralId}/jfl/${comando}`,
            { particao: p.numero },
            { ...cabecalhos, 'Idempotency-Key': `${cabecalhos['Idempotency-Key']}-p${p.numero}` },
          );
          if (!r.sucesso) throw new Error(r.mensagemErro ?? 'A central recusou o comando.');
          ultimoStatus = r.statusResultante ?? ultimoStatus;
        }
        return { ultimoStatus };
      };
      // Desarmar pede o PIN (ou a digital/rosto); armar não.
      const envio =
        comando === 'desarmar'
          ? await confirmarComando('Desarmar o alarme', enviarTodas)
          : await enviarTodas({ 'Idempotency-Key': novaChave() });
      if (envio === null) {
        return; // cancelou a confirmação
      }
      const ultimo = envio.ultimoStatus;
      if (ultimo) {
        setStatus(ultimo);
        setAtualizadoEm(new Date());
      } else {
        await lerStatus();
      }
    } catch (e) {
      ServicoFeedbackTatil.notificationError();
      setErro(e instanceof Error ? e.message : 'Não foi possível enviar o comando.');
    } finally {
      setEnviando(null);
    }
  };

  let estado: EstadoAlarme;
  if (centrais === null || (lendo && !status)) estado = 'carregando';
  else if (centrais.length === 0) estado = 'semCentral';
  else if (!status) estado = 'semComunicacao';
  else estado = estadoDasParticoes(status.particoes);

  return {
    estado,
    centrais: centrais ?? [],
    central,
    centralId,
    selecionarCentral: setCentralId,
    particoes,
    lendo,
    enviando,
    erro: central?.status === 'Offline' ? null : erro,
    atualizadoEm,
    comandar,
    lerStatus,
  };
}
