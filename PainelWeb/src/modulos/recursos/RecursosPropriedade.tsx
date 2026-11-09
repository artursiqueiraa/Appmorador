import { Alert, Box, Skeleton, Switch, Typography } from '@mui/material';
import { extrairMensagemErro } from '../../services/httpClient';
import { useToastStore } from '../../stores/toastStore';
import { useDefinirRecursoMutation, useRecursosQuery } from './queries/useRecursos';
import type { RecursoPropriedade } from './types';

const RECURSOS: { valor: RecursoPropriedade; rotulo: string; descricao: string }[] = [
  { valor: 'Cameras', rotulo: 'Câmeras', descricao: 'Ver as câmeras no app.' },
  { valor: 'Snapshot', rotulo: 'Foto no disparo', descricao: 'Imagem automática quando o alarme dispara.' },
  { valor: 'StreamingAoVivo', rotulo: 'Vídeo ao vivo', descricao: 'Transmissão em tempo real.' },
  { valor: 'Push', rotulo: 'Notificações', descricao: 'Avisos no celular.' },
  { valor: 'Facial', rotulo: 'Acesso facial', descricao: 'Cadastro e liberação pelo rosto.' },
  { valor: 'Pgm', rotulo: 'Acionamentos', descricao: 'Abrir portão e acionar saídas pelo app.' },
  { valor: 'InterfoneSip', rotulo: 'Interfone', descricao: 'Atender o interfone pelo app.' },
  { valor: 'Ia', rotulo: 'Análise inteligente', descricao: 'Recursos de inteligência artificial.' },
];

/**
 * Akteck Seg (ADR 0035) — "o que esta propriedade contratou" (feature flags, ADR 0026). Cada
 * interruptor salva na hora; durante o envio ele fica desabilitado para não disparar duas vezes.
 */
export function RecursosPropriedade({ propriedadeId }: { propriedadeId: string }) {
  const mostrarToast = useToastStore((s) => s.mostrar);
  const { data: ativos, isLoading, isError } = useRecursosQuery(propriedadeId);
  const definir = useDefinirRecursoMutation(propriedadeId);

  if (isLoading) {
    return <Skeleton variant="rounded" height={120} />;
  }
  if (isError) {
    return <Alert severity="error">Não foi possível carregar os recursos desta propriedade.</Alert>;
  }

  const alterar = (recurso: RecursoPropriedade, ativo: boolean, rotulo: string) =>
    definir.mutate(
      { recurso, ativo },
      {
        onSuccess: () => mostrarToast(`${rotulo} ${ativo ? 'ativado' : 'desativado'}.`, 'success'),
        onError: (e) => mostrarToast(extrairMensagemErro(e, 'Não foi possível alterar o recurso.'), 'error'),
      },
    );

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' }, columnGap: 3 }}>
      {RECURSOS.map((r) => {
        const ligado = (ativos ?? []).includes(r.valor);
        const enviando = definir.isPending && definir.variables?.recurso === r.valor;
        return (
          <Box key={r.valor} sx={{ display: 'flex', alignItems: 'center', gap: 2, py: 1, borderTop: 1, borderColor: 'divider' }}>
            <Box sx={{ flexGrow: 1, minWidth: 0 }}>
              <Typography id={`recurso-${propriedadeId}-${r.valor}`}>{r.rotulo}</Typography>
              <Typography variant="body2" color="text.secondary">
                {r.descricao}
              </Typography>
            </Box>
            <Switch
              checked={ligado}
              disabled={enviando}
              onChange={(_, valor) => alterar(r.valor, valor, r.rotulo)}
              slotProps={{ input: { 'aria-labelledby': `recurso-${propriedadeId}-${r.valor}` } }}
            />
          </Box>
        );
      })}
    </Box>
  );
}
