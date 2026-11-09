import { Box, Paper, Skeleton, Typography } from '@mui/material';

export interface ItemDistribuicao {
  chave: string;
  rotulo: string;
  valor: number;
  cor: string;
}

/**
 * Akteck Seg (ADR 0035) — distribuição em barras horizontais com o número escrito ao lado.
 * Substitui as rosquinhas, onde comparar fatias exigia adivinhar ângulos e a legenda
 * ficava separada dos valores.
 */
export function DistribuicaoBarras({
  titulo,
  descricao,
  itens,
  carregando,
  vazio,
}: {
  titulo: string;
  descricao: string;
  itens: ItemDistribuicao[];
  carregando: boolean;
  vazio: string;
}) {
  const total = itens.reduce((s, i) => s + i.valor, 0);

  return (
    <Paper variant="outlined" sx={{ p: 2.5, height: '100%' }}>
      <Typography variant="h3" component="h2">
        {titulo}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {descricao}
      </Typography>
      {carregando ? (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} variant="rounded" height={28} />
          ))}
        </Box>
      ) : total === 0 ? (
        <Typography color="text.secondary">{vazio}</Typography>
      ) : (
        <Box component="dl" sx={{ m: 0, display: 'flex', flexDirection: 'column', gap: 1.75 }}>
          {itens.map((item) => {
            const pct = Math.round((item.valor / total) * 100);
            return (
              <Box key={item.chave}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                  <Typography component="dt" variant="body2">
                    {item.rotulo}
                  </Typography>
                  <Typography component="dd" variant="body2" sx={{ m: 0, fontWeight: 600 }}>
                    {item.valor.toLocaleString('pt-BR')}{' '}
                    <Box component="span" sx={{ color: 'text.secondary', fontWeight: 400 }}>
                      ({pct}%)
                    </Box>
                  </Typography>
                </Box>
                <Box sx={{ height: 8, borderRadius: 999, bgcolor: 'marca.surfaceAlt', overflow: 'hidden' }} aria-hidden="true">
                  <Box sx={{ height: '100%', width: `${pct}%`, bgcolor: item.cor, borderRadius: 999 }} />
                </Box>
              </Box>
            );
          })}
        </Box>
      )}
    </Paper>
  );
}
