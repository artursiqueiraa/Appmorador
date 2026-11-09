import { Paper, Skeleton, Typography, useTheme } from '@mui/material';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { NovosClientesPorMesItem } from '../../types/api';

const MESES = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

/** "2026-07" → "jul/26". */
function rotuloMes(mes: string): string {
  const [ano, m] = mes.split('-');
  const indice = Number(m) - 1;
  return MESES[indice] ? `${MESES[indice]}/${ano.slice(2)}` : mes;
}

/** Akteck Seg (ADR 0035) — eixos, grade e tooltip agora seguem o tema (antes ficavam com as cores do escuro no modo claro). */
export function GraficoNovosClientes({ dados, carregando }: { dados: NovosClientesPorMesItem[]; carregando: boolean }) {
  const theme = useTheme();
  const m = theme.palette.marca;
  const serie = dados.map((d) => ({ mes: rotuloMes(d.mes), quantidade: d.quantidade }));

  return (
    <Paper variant="outlined" sx={{ p: 2.5, height: '100%' }}>
      <Typography variant="h3" component="h2">
        Novos clientes por mês
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Contas criadas em cada mês.
      </Typography>
      {carregando ? (
        <Skeleton variant="rounded" height={220} />
      ) : serie.length === 0 ? (
        <Typography color="text.secondary">Ainda não há clientes cadastrados.</Typography>
      ) : (
        <ResponsiveContainer width="100%" height={220}>
          <BarChart data={serie} margin={{ top: 4, right: 4, left: -16, bottom: 0 }}>
            <CartesianGrid vertical={false} stroke={m.border} />
            <XAxis dataKey="mes" tickLine={false} axisLine={{ stroke: m.border }} tick={{ fill: m.textSecondary, fontSize: 12 }} />
            <YAxis allowDecimals={false} tickLine={false} axisLine={false} tick={{ fill: m.textSecondary, fontSize: 12 }} />
            <Tooltip
              cursor={{ fill: m.surfaceHover }}
              contentStyle={{ background: m.surface, border: `1px solid ${m.border}`, borderRadius: 8, color: m.textPrimary }}
              labelStyle={{ color: m.textSecondary }}
              formatter={(valor) => [valor, 'Novos clientes']}
            />
            <Bar dataKey="quantidade" fill={m.primary} radius={[4, 4, 0, 0]} maxBarSize={48} isAnimationActive={false} />
          </BarChart>
        </ResponsiveContainer>
      )}
    </Paper>
  );
}
