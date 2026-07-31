import { Chip, MenuItem, TextField } from '@mui/material';

export type CorStatus = 'success' | 'warning' | 'error' | 'info' | 'default';

export interface OpcaoStatus<T extends string> {
  valor: T;
  rotulo: string;
  cor: CorStatus;
}

interface BadgeStatusProps<T extends string> {
  valor: T;
  opcoes: readonly OpcaoStatus<T>[];
}

/** Sprint 22B (ADR 0031) — Chip colorido a partir de um mapa enum→cor, reutilizado pelos 3 módulos novos. */
export function BadgeStatus<T extends string>({ valor, opcoes }: BadgeStatusProps<T>) {
  const opcao = opcoes.find((o) => o.valor === valor);
  return <Chip size="small" label={opcao?.rotulo ?? valor} color={opcao?.cor ?? 'default'} />;
}

interface SeletorStatusProps<T extends string> {
  value: T | '';
  onChange: (valor: T | '') => void;
  opcoes: readonly OpcaoStatus<T>[];
  label: string;
  todosRotulo?: string;
}

/** Sprint 22B (ADR 0031) — Select para filtrar/escolher um valor de enum, com opção "Todos" opcional. */
export function SeletorStatus<T extends string>({
  value,
  onChange,
  opcoes,
  label,
  todosRotulo,
}: SeletorStatusProps<T>) {
  return (
    <TextField
      select
      size="small"
      label={label}
      value={value}
      onChange={(e) => onChange(e.target.value as T | '')}
      sx={{ minWidth: 180 }}
    >
      {todosRotulo ? <MenuItem value="">{todosRotulo}</MenuItem> : null}
      {opcoes.map((opcao) => (
        <MenuItem key={opcao.valor} value={opcao.valor}>
          {opcao.rotulo}
        </MenuItem>
      ))}
    </TextField>
  );
}
