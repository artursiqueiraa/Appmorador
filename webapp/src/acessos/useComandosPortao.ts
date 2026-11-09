import { useCallback, useEffect, useRef, useState } from 'react';
import { api, ApiError } from '../api/client';
import type { EquipamentoResponse, ResultadoComandoJfl } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { usePermissao } from '../auth/usePermissao';
import { useToast } from '../components/Toast';
import { useRealtimeSnapshot } from '../realtime/RealtimeContext';
import { obterRotulos, rotuloPadrao, type RotuloPgm } from './pgmLabels';
import { confirmarComando } from '../seguranca/confirmarComando';

export interface ComandoPortao {
  equipamentoId: string;
  numeroPgm: number;
  rotulo: RotuloPgm;
  conectado: boolean;
  descricaoEstado: string;
}

/**
 * Sprint 17 (ADR 0020) — só centrais JFL têm PGM de verdade hoje (Intelbras/Control iD não
 * suportam): nunca inventar comandos para fabricantes sem essa capacidade.
 */
async function carregarComandosJfl(propriedadeId: string): Promise<ComandoPortao[]> {
  const equipamentos = await api.get<EquipamentoResponse[]>(`/api/properties/${propriedadeId}/equipamentos`);
  const centraisJfl = equipamentos.filter((eq) => eq.fabricante === 'Jfl');

  const porCentral = await Promise.all(
    centraisJfl.map(async (equipamento) => {
      try {
        const resultado = await api.get<ResultadoComandoJfl>(`/api/equipamentos/${equipamento.id}/jfl/status`);
        if (!resultado.sucesso || !resultado.statusResultante) {
          return [];
        }
        const rotulos = await obterRotulos(equipamento.id);
        const conectado = equipamento.status === 'Online';
        return resultado.statusResultante.pgms
          .filter((pgm) => pgm.permitida)
          .map(
            (pgm): ComandoPortao => ({
              equipamentoId: equipamento.id,
              numeroPgm: pgm.numero,
              rotulo: rotulos[pgm.numero] ?? rotuloPadrao(pgm.numero),
              conectado,
              descricaoEstado: conectado ? 'Pronto' : 'Sem comunicação com o equipamento',
            }),
          );
      } catch {
        return [];
      }
    }),
  );
  return porCentral.flat();
}

/**
 * Akteck Seg (ADR 0038) — "abrir portão" (PGM da central), extraído da aba Acessos para poder
 * aparecer no Início. Mesma lógica de antes: só JFL, respeita a permissão `AbrirPortao`, estado
 * Online/Offline atualizado ao vivo pelo snapshot (SignalR) e erro com "Tentar novamente".
 */
export function useComandosPortao() {
  const { selectedProperty } = useAuth();
  const { temPermissao } = usePermissao();
  const podeAbrirPortao = temPermissao('AbrirPortao');
  const { mostrarErro } = useToast();
  const { ultimoSnapshot } = useRealtimeSnapshot();
  const [comandos, setComandos] = useState<ComandoPortao[]>([]);
  const [carregando, setCarregando] = useState(true);

  const carregar = useCallback(async () => {
    if (!selectedProperty || !podeAbrirPortao) {
      setComandos([]);
      setCarregando(false);
      return;
    }
    setCarregando(true);
    try {
      setComandos(await carregarComandosJfl(selectedProperty.id));
    } catch {
      setComandos([]);
    } finally {
      setCarregando(false);
    }
  }, [selectedProperty, podeAbrirPortao]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  // Troca de propriedade: nunca mostrar portões da casa anterior.
  const propriedadeAnteriorRef = useRef(selectedProperty?.id ?? null);
  useEffect(() => {
    const novo = selectedProperty?.id ?? null;
    if (propriedadeAnteriorRef.current !== novo) {
      propriedadeAnteriorRef.current = novo;
      setComandos([]);
    }
  }, [selectedProperty?.id]);

  // Sprint 18 (ADR 0022, Fase 6) — Online/Offline atualiza sozinho pelo snapshot.
  useEffect(() => {
    if (!ultimoSnapshot || !selectedProperty || ultimoSnapshot.propriedadeId !== selectedProperty.id) {
      return;
    }
    const estadoPorEquipamento = new Map(ultimoSnapshot.snapshot.equipamentos.map((eq) => [eq.equipamentoId, eq.estado]));
    setComandos((atual) =>
      atual.map((comando) => {
        const estado = estadoPorEquipamento.get(comando.equipamentoId);
        if (!estado) return comando;
        const conectado = estado !== 'Offline';
        return conectado === comando.conectado ? comando : { ...comando, conectado, descricaoEstado: conectado ? 'Pronto' : 'Sem comunicação com o equipamento' };
      }),
    );
  }, [ultimoSnapshot, selectedProperty]);

  const acionar = useCallback(
    async (comando: ComandoPortao): Promise<boolean> => {
      try {
        // Sprint 27 (ADR 0043) — acionar saída (portão) pede o PIN ou a digital/rosto.
        const resultado = await confirmarComando(comando.rotulo.label, (cab) =>
          api.post<ResultadoComandoJfl>(`/api/equipamentos/${comando.equipamentoId}/jfl/pgm/acionar`, { pgmNumero: comando.numeroPgm }, cab),
        );
        if (resultado === null) {
          return false;
        }
        if (!resultado.sucesso) {
          mostrarErro({
            titulo: 'Não foi possível acionar',
            mensagem: resultado.mensagemErro ?? 'O equipamento não respondeu. Tente novamente.',
            onRetry: () => acionar(comando),
          });
        }
        return resultado.sucesso;
      } catch (err) {
        mostrarErro({
          titulo: 'Não foi possível acionar',
          mensagem: err instanceof ApiError ? err.message : 'Algo deu errado. Tente novamente.',
          onRetry: () => acionar(comando),
        });
        return false;
      }
    },
    [mostrarErro],
  );

  return { comandos, carregando, podeAbrirPortao, acionar, recarregar: carregar };
}
