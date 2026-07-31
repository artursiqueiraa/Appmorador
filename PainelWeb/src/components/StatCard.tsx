import { Card, CardActionArea, CardContent, Typography, Box, Skeleton } from '@mui/material';
import type { SvgIconComponent } from '@mui/icons-material';

interface StatCardProps {
  titulo: string;
  valor: number | string;
  icone: SvgIconComponent;
  cor?: 'primary' | 'error' | 'warning' | 'success';
  carregando?: boolean;
  onClick?: () => void;
}

/** Sprint 22A (Fase 3) — card de ação imediata: sempre clicável quando `onClick` é passado, leva à tela filtrada. */
export function StatCard({ titulo, valor, icone: Icone, cor = 'primary', carregando, onClick }: StatCardProps) {
  const conteudo = (
    <CardContent>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Box>
          <Typography variant="caption" color="text.secondary">
            {titulo}
          </Typography>
          {carregando ? (
            <Skeleton variant="text" width={60} height={40} />
          ) : (
            <Typography variant="h1" sx={{ fontWeight: 800 }}>
              {valor}
            </Typography>
          )}
        </Box>
        <Icone sx={{ fontSize: 36 }} color={cor} />
      </Box>
    </CardContent>
  );

  return (
    <Card variant="outlined" sx={{ height: '100%' }}>
      {onClick ? <CardActionArea onClick={onClick}>{conteudo}</CardActionArea> : conteudo}
    </Card>
  );
}
