import { Box, Typography } from '@mui/material';

/**
 * Identidade Akteck Seg (ADR 0035) — símbolo: escudo com um "A" em chevron vazado.
 * Desenhado em `currentColor` para herdar a cor do contexto (marca no menu, branco
 * sobre fundo de marca etc.). O arquivo estático equivalente é `public/favicon.svg`.
 */
export function SimboloAkteck({ tamanho = 28 }: { tamanho?: number }) {
  return (
    <Box
      component="svg"
      viewBox="0 0 32 32"
      width={tamanho}
      height={tamanho}
      aria-hidden="true"
      sx={{ color: 'primary.main', flexShrink: 0, display: 'block' }}
    >
      <path d="M16 2 27 6v9c0 7-4.5 12.5-11 15C9.5 27.5 5 22 5 15V6l11-4Z" fill="currentColor" />
      <path
        d="M11 21.5 16 10l5 11.5M13.2 17h5.6"
        fill="none"
        stroke="var(--marca-recorte)"
        strokeWidth="2.4"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </Box>
  );
}

/** Símbolo + "Akteck Seg". `compacta` mostra só o símbolo (ex.: espaço reduzido). */
export function MarcaAkteck({ tamanho = 28, compacta = false }: { tamanho?: number; compacta?: boolean }) {
  return (
    <Box
      sx={(theme) => ({
        display: 'flex',
        alignItems: 'center',
        gap: 1.25,
        '--marca-recorte': theme.palette.marca.background,
      })}
      aria-label="Akteck Seg"
      role="img"
    >
      <SimboloAkteck tamanho={tamanho} />
      {compacta ? null : (
        <Typography component="span" sx={{ fontSize: tamanho * 0.64, fontWeight: 700, color: 'text.primary', whiteSpace: 'nowrap' }}>
          Akteck{' '}
          <Box component="span" sx={{ fontWeight: 500, color: 'text.secondary' }}>
            Seg
          </Box>
        </Typography>
      )}
    </Box>
  );
}
