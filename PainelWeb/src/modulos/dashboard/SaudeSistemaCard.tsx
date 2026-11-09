import { Alert, Box, Chip, Paper, Skeleton, Typography, useTheme } from '@mui/material';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlined';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutlined';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import type { RegistroBackup, SaudeSistema, SituacaoComponente } from '../../services/saudeService';
import { formatarDataHora } from './rotulosDashboard';

const ROTULO: Record<SituacaoComponente, string> = { Ok: 'Normal', Atencao: 'Atenção', Fora: 'Fora do ar' };

function Icone({ situacao }: { situacao: SituacaoComponente }) {
  const theme = useTheme();
  if (situacao === 'Ok') return <CheckCircleOutlineIcon fontSize="small" sx={{ color: theme.palette.success.main }} />;
  if (situacao === 'Atencao') return <WarningAmberIcon fontSize="small" sx={{ color: theme.palette.warning.main }} />;
  return <ErrorOutlineIcon fontSize="small" sx={{ color: theme.palette.error.main }} />;
}

function LinhaBackup({ rotulo, registro }: { rotulo: string; registro?: RegistroBackup | null }) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
      <Typography variant="body2" color="text.secondary">
        {rotulo}
      </Typography>
      <Typography variant="body2" color={registro && !registro.sucesso ? 'error.main' : 'text.primary'}>
        {registro
          ? `${registro.sucesso ? 'OK' : 'Falhou'} · ${formatarDataHora(registro.ocorridoEmUtc)}${registro.mensagem && registro.sucesso ? ` · ${registro.mensagem}` : ''}`
          : 'Nunca executado'}
      </Typography>
    </Box>
  );
}

/**
 * Sprint 27 (ADR 0043) — saúde do sistema: banco, recepção das centrais, rotinas em segundo plano
 * e backup. Componente fora há mais de 2 minutos também gera e-mail ao Master. Se o próprio
 * backend cair, esta consulta falha e o card avisa.
 */
export function SaudeSistemaCard({ dados, carregando, erro }: { dados?: SaudeSistema; carregando: boolean; erro: boolean }) {
  const comProblema = dados?.componentes.filter((c) => c.situacao !== 'Ok') ?? [];

  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.5, borderColor: erro || dados?.geral === 'Fora' ? 'error.main' : dados?.geral === 'Atencao' ? 'warning.main' : undefined }}
    >
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 2, mb: 1.5, flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="h3" component="h2">
            Saúde do sistema
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Verificado a cada minuto. Algo fora por mais de 2 minutos avisa o Master por e-mail.
          </Typography>
        </Box>
        {dados ? (
          <Chip
            icon={<Icone situacao={dados.geral} />}
            label={dados.geral === 'Ok' ? 'Tudo funcionando' : ROTULO[dados.geral]}
            variant="outlined"
            color={dados.geral === 'Ok' ? 'success' : dados.geral === 'Atencao' ? 'warning' : 'error'}
          />
        ) : null}
      </Box>

      {erro ? (
        <Alert severity="error">Não foi possível falar com o servidor. Ele pode estar fora do ar.</Alert>
      ) : carregando || !dados ? (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} variant="text" height={32} />
          ))}
        </Box>
      ) : (
        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'repeat(2, minmax(0, 1fr))' } }}>
          <Box component="ul" sx={{ listStyle: 'none', m: 0, p: 0, display: 'flex', flexDirection: 'column', gap: 1 }}>
            {dados.componentes.map((c) => (
              <Box component="li" key={c.chave} sx={{ display: 'flex', gap: 1, alignItems: 'flex-start' }}>
                <Box sx={{ pt: '2px' }}>
                  <Icone situacao={c.situacao} />
                </Box>
                <Box sx={{ minWidth: 0 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {c.nome} <Typography component="span" variant="body2" color="text.secondary">· {ROTULO[c.situacao]}</Typography>
                  </Typography>
                  {c.detalhe ? (
                    <Typography variant="caption" color={c.situacao === 'Ok' ? 'text.secondary' : c.situacao === 'Fora' ? 'error.main' : 'warning.main'}>
                      {c.detalhe}
                      {c.foraDesdeUtc ? ` Desde ${formatarDataHora(c.foraDesdeUtc)}.` : ''}
                    </Typography>
                  ) : null}
                </Box>
              </Box>
            ))}
          </Box>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              Backup do banco
            </Typography>
            <LinhaBackup rotulo="Último backup" registro={dados.ultimoBackup} />
            <LinhaBackup rotulo="Último teste de restauração" registro={dados.ultimoTesteRestauracao} />
            {comProblema.length === 0 ? (
              <Typography variant="caption" color="text.secondary">
                Backup diário às 3h pelo Agendador de Tarefas do Windows, com teste de restauração.
              </Typography>
            ) : null}
          </Box>
        </Box>
      )}
    </Paper>
  );
}
