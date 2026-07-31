import { useQuery } from '@tanstack/react-query';
import { Box, Typography, Paper, Grid, Chip, Skeleton, Card, CardMedia, CardContent } from '@mui/material';
import VideocamOffIcon from '@mui/icons-material/VideocamOff';
import { propriedadesService } from '../services/propriedadesService';
import { useAuthStore } from '../stores/authStore';
import { useAuthenticatedImage } from '../hooks/useAuthenticatedImage';
import { EmptyState } from '../components/EmptyState';

function CameraThumb({
  camera,
}: {
  camera: { id: string; nome: string; ultimaImagemUrl?: string | null; status: string };
}) {
  const url = useAuthenticatedImage(camera.ultimaImagemUrl);
  return (
    <Card variant="outlined">
      {url ? (
        <CardMedia component="img" height={120} image={url} alt={camera.nome} />
      ) : (
        <Box
          sx={{ height: 120, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'action.hover' }}
        >
          <VideocamOffIcon color="disabled" />
        </Box>
      )}
      <CardContent sx={{ py: 1 }}>
        <Typography variant="body1">{camera.nome}</Typography>
        <Chip size="small" label={camera.status} color={camera.status === 'Online' ? 'success' : 'default'} />
      </CardContent>
    </Card>
  );
}

/** Sprint 22A (Fase 6) — snapshot rápido da propriedade sendo diagnosticada, durante impersonation. */
export function SuporteDiagnosticoPage() {
  const impersonation = useAuthStore((s) => s.impersonation);
  const propriedadeId = impersonation?.propriedadeId;

  const { data: snapshot, isLoading: carregandoSnapshot } = useQuery({
    queryKey: ['snapshot-operacional', propriedadeId],
    queryFn: () => propriedadesService.obterSnapshotOperacional(propriedadeId!),
    enabled: Boolean(propriedadeId),
  });

  const { data: cameras, isLoading: carregandoCameras } = useQuery({
    queryKey: ['cameras', propriedadeId],
    queryFn: () => propriedadesService.listarCameras(propriedadeId!),
    enabled: Boolean(propriedadeId),
  });

  const { data: eventos } = useQuery({
    queryKey: ['ultimos-eventos', propriedadeId],
    queryFn: () => propriedadesService.listarUltimosEventos(propriedadeId!),
    enabled: Boolean(propriedadeId),
  });

  if (!impersonation) {
    return (
      <EmptyState
        icone={VideocamOffIcon}
        titulo="Nenhuma sessão de suporte ativa"
        descricao="Entre como um cliente em Clientes/Suporte para ver o diagnóstico da propriedade dele."
      />
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Typography variant="h1">Diagnóstico — {impersonation.propriedadeNome}</Typography>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="caption" color="text.secondary">
              Saúde Operacional
            </Typography>
            {carregandoSnapshot ? (
              <Skeleton width={100} height={32} />
            ) : (
              <Box>
                <Chip label={snapshot?.saude ?? '—'} color={snapshot?.saude === 'Saudavel' ? 'success' : 'warning'} />
              </Box>
            )}
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="caption" color="text.secondary">
              Último Heartbeat
            </Typography>
            <Typography variant="body1">
              {snapshot?.ultimaComunicacaoUtc
                ? new Date(snapshot.ultimaComunicacaoUtc).toLocaleString('pt-BR')
                : 'Sem comunicação registrada'}
            </Typography>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="caption" color="text.secondary">
              Equipamentos Online/Offline
            </Typography>
            <Typography variant="body1">
              {snapshot?.quantidadeEquipamentosOnline ?? 0} online / {snapshot?.quantidadeEquipamentosOffline ?? 0}{' '}
              offline
            </Typography>
          </Paper>
        </Grid>
      </Grid>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="h3" sx={{ mb: 2 }}>
          Câmeras
        </Typography>
        {carregandoCameras ? (
          <Skeleton variant="rounded" height={140} />
        ) : cameras && cameras.length > 0 ? (
          <Grid container spacing={2}>
            {cameras.map((camera) => (
              <Grid key={camera.id} size={{ xs: 6, sm: 4, md: 3 }}>
                <CameraThumb camera={camera} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <EmptyState
            icone={VideocamOffIcon}
            titulo="Nenhuma câmera"
            descricao="Esta propriedade não tem câmeras configuradas."
          />
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="h3" sx={{ mb: 1 }}>
          Último Evento Recebido
        </Typography>
        {eventos && eventos.length > 0 ? (
          <Typography variant="body1">
            {eventos[0].titulo} — {new Date(eventos[0].ocorridoEmUtc).toLocaleString('pt-BR')}
          </Typography>
        ) : (
          <Typography variant="body1" color="text.secondary">
            Nenhum evento registrado ainda.
          </Typography>
        )}
      </Paper>
    </Box>
  );
}
