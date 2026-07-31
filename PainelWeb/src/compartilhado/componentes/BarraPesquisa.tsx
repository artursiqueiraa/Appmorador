import { TextField, InputAdornment } from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';

interface BarraPesquisaProps {
  value: string;
  onChange: (valor: string) => void;
  placeholder?: string;
}

/** Sprint 22B (ADR 0031) — campo de busca padrão dos módulos administrativos (o debounce fica a cargo de quem consome, via `useDebounce`). */
export function BarraPesquisa({ value, onChange, placeholder = 'Buscar' }: BarraPesquisaProps) {
  return (
    <TextField
      placeholder={placeholder}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      size="small"
      sx={{ maxWidth: 360 }}
      slotProps={{
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon />
            </InputAdornment>
          ),
        },
      }}
    />
  );
}
