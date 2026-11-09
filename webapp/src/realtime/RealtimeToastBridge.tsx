import { useEffect, useRef } from 'react';
import { useToast } from '../components/Toast';
import { useTelaAtiva } from '../navigation/telaAtivaStore';
import { useRealtimeEvento } from './RealtimeContext';

/**
 * Sprint 18 (ADR 0022, Fase 3 — Toasts Inteligentes, Regra 1 — Toast Inteligente)
 * — componente sem UI própria, montado uma vez perto da raiz da navegação.
 * Decide se um evento em tempo real merece um toast: NÃO mostra quando a tela em
 * foco já torna o evento visível por si só (Início — Atividade recente ao vivo;
 * Eventos — Timeline ao vivo, ambos Fase 1/2 desta Sprint); mostra um toast
 * discreto quando o evento aconteceu "fora do contexto atual" (o morador está em
 * Câmeras, Ajustes, Acessos ou qualquer tela de detalhe).
 */
const TELAS_COM_VISIBILIDADE_PROPRIA = new Set(['Inicio', 'Eventos']);

export function RealtimeToastBridge() {
  const { ultimoEvento } = useRealtimeEvento();
  const { mostrarToast } = useToast();
  const telaAtiva = useTelaAtiva();
  const ultimoIdProcessadoRef = useRef<string | null>(null);

  useEffect(() => {
    if (!ultimoEvento) {
      return;
    }

    if (ultimoIdProcessadoRef.current === ultimoEvento.evento.id) {
      return;
    }
    ultimoIdProcessadoRef.current = ultimoEvento.evento.id;

    if (telaAtiva && TELAS_COM_VISIBILIDADE_PROPRIA.has(telaAtiva)) {
      return;
    }

    mostrarToast({
      tipo: ultimoEvento.evento.destaque ? 'alerta' : 'info',
      titulo: ultimoEvento.evento.destaque ? 'Atenção' : 'Atualização',
      mensagem: ultimoEvento.evento.titulo,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ultimoEvento, telaAtiva]);

  return null;
}
