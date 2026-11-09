import { Box, ButtonBase, Paper, Skeleton, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import type { DashboardOperacionalResponse } from '../../types/api';

interface Indicador {
  rotulo: string;
  valor: number;
  detalhe: string;
  destino?: string;
  alerta?: boolean;
}

/**
 * Akteck Seg (ADR 0035) — linha de indicadores: número grande em algarismos tabulares e
 * um rótulo. Sem ícone decorativo e sem seta de tendência que ninguém mediu. Cada
 * indicador leva à tela onde aquele número pode ser trabalhado.
 */
export function IndicadoresOperacionais({
  dados,
  carregando,
  podeVerEquipamentos,
}: {
  dados?: DashboardOperacionalResponse;
  carregando: boolean;
  podeVerEquipamentos: boolean;
}) {
  const navigate = useNavigate();
  const offline = dados?.totalEquipamentosOffline ?? 0;
  const total = dados?.totalEquipamentos ?? 0;

  const indicadores: Indicador[] = [
    { rotulo: 'Clientes', valor: dados?.totalClientes ?? 0, detalhe: 'contas de clientes', destino: '/clientes' },
    { rotulo: 'Propriedades', valor: dados?.totalPropriedades ?? 0, detalhe: 'casas e negócios protegidos' },
    {
      rotulo: 'Equipamentos',
      valor: total,
      detalhe: total > 0 ? `${total - offline} online agora` : 'nenhum instalado ainda',
      destino: podeVerEquipamentos ? '/equipamentos' : undefined,
    },
    {
      rotulo: 'Equipamentos offline',
      valor: offline,
      detalhe: offline > 0 ? 'precisam de verificação' : 'tudo respondendo',
      destino: podeVerEquipamentos ? '/diagnostico-equipamentos' : undefined,
      alerta: offline > 0,
    },
  ];

  return (
    <Paper variant="outlined" sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', md: 'repeat(4, minmax(0, 1fr))' } }}>
      {indicadores.map((ind, i) => {
        const conteudo = (
          <Box sx={{ p: 2.5, textAlign: 'left', width: '100%' }}>
            <Typography variant="body2" color="text.secondary">
              {ind.rotulo}
            </Typography>
            {carregando ? (
              <Skeleton variant="text" width={64} sx={{ fontSize: 30 }} />
            ) : (
              <Typography sx={{ fontSize: 30, fontWeight: 700, lineHeight: '38px', color: ind.alerta ? 'error.main' : 'text.primary' }}>
                {ind.valor.toLocaleString('pt-BR')}
              </Typography>
            )}
            <Typography variant="caption" color="text.secondary">
              {carregando ? ' ' : ind.detalhe}
            </Typography>
          </Box>
        );
        const borda = {
          borderLeft: { md: i === 0 ? 0 : 1 },
          borderTop: { xs: i >= 2 ? 1 : 0, md: 0 },
          borderRight: { xs: i % 2 === 0 ? 1 : 0, md: 0 },
          borderColor: 'divider',
        };
        return ind.destino ? (
          <ButtonBase
            key={ind.rotulo}
            onClick={() => navigate(ind.destino!)}
            sx={{ ...borda, display: 'flex', alignItems: 'flex-start', justifyContent: 'flex-start', '&:hover': { bgcolor: 'action.hover' } }}
            aria-label={`${ind.rotulo}: ${ind.valor}. Abrir`}
          >
            {conteudo}
          </ButtonBase>
        ) : (
          <Box key={ind.rotulo} sx={borda}>
            {conteudo}
          </Box>
        );
      })}
    </Paper>
  );
}
