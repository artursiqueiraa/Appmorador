import { useMutation } from '@tanstack/react-query';
import { Alert, Box, Button, Typography } from '@mui/material';
import { httpClient, extrairMensagemErro } from '../../services/httpClient';
import { useToastStore } from '../../stores/toastStore';

export type TipoEventoSimulado = 'Disparo' | 'Panico' | 'Violacao' | 'SemComunicacao' | 'FalhaEnergia' | 'BateriaBaixa';

interface SimularEventoResponse {
  mensagem: string;
  alertaId?: string | null;
}

const EVENTOS: { tipo: TipoEventoSimulado; rotulo: string; critico: boolean }[] = [
  { tipo: 'Disparo', rotulo: 'Alarme disparado', critico: true },
  { tipo: 'Panico', rotulo: 'Pânico', critico: true },
  { tipo: 'Violacao', rotulo: 'Violação da central', critico: true },
  { tipo: 'SemComunicacao', rotulo: 'Central sem comunicação', critico: true },
  { tipo: 'FalhaEnergia', rotulo: 'Falta de energia', critico: false },
  { tipo: 'BateriaBaixa', rotulo: 'Bateria fraca', critico: false },
];

const simuladorAdaptador = {
  simular: (propriedadeId: string, tipo: TipoEventoSimulado) =>
    httpClient.post<SimularEventoResponse>(`/api/painel/simulador/propriedades/${propriedadeId}/eventos`, { tipo }).then((r) => r.data),
};

/**
 * Sprint 24 (ADR 0040) — dispara um evento de teste na propriedade para homologar alertas sem
 * central física. Os críticos abrem a tela de alerta no app, com escalonamento real; os outros só
 * notificam. Tudo sai marcado "Teste:" para o cliente e fica na auditoria.
 */
export function SimuladorEventos({ propriedadeId }: { propriedadeId: string }) {
  const mostrarToast = useToastStore((s) => s.mostrar);
  const simular = useMutation({
    mutationFn: (tipo: TipoEventoSimulado) => simuladorAdaptador.simular(propriedadeId, tipo),
    onSuccess: (r) => mostrarToast(r.mensagem, r.mensagem.startsWith('Já existe') ? 'warning' : 'success'),
    onError: (e) => mostrarToast(extrairMensagemErro(e, 'Não foi possível simular o evento.'), 'error'),
  });

  return (
    <Box>
      <Alert severity="warning" sx={{ mb: 1.5 }}>
        O cliente recebe de verdade: tela de alerta no app e, sem confirmação, aviso aos contatos de emergência. Use só em
        propriedades de teste ou com o cliente avisado.
      </Alert>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        {EVENTOS.map((e) => (
          <Button
            key={e.tipo}
            size="small"
            variant="outlined"
            color={e.critico ? 'error' : 'inherit'}
            loading={simular.isPending && simular.variables === e.tipo}
            disabled={simular.isPending && simular.variables !== e.tipo}
            onClick={() => simular.mutate(e.tipo)}
          >
            {e.rotulo}
          </Button>
        ))}
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
        Vermelhos abrem alerta crítico (com escalonamento); os outros só enviam notificação.
      </Typography>
    </Box>
  );
}
