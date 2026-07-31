import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import PeopleIcon from '@mui/icons-material/People';
import { StatCard } from '../StatCard';

describe('StatCard', () => {
  it('carregando: mostra skeleton, não o valor', () => {
    render(<StatCard titulo="Total de Clientes" valor={42} icone={PeopleIcon} carregando />);

    expect(screen.queryByText('42')).not.toBeInTheDocument();
  });

  it('sem carregar: mostra o valor', () => {
    render(<StatCard titulo="Total de Clientes" valor={42} icone={PeopleIcon} />);

    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('Total de Clientes')).toBeInTheDocument();
  });

  it('com onClick: card é clicável e chama o handler', () => {
    const onClick = vi.fn();
    render(<StatCard titulo="Total" valor={10} icone={PeopleIcon} onClick={onClick} />);

    fireEvent.click(screen.getByRole('button'));

    expect(onClick).toHaveBeenCalledOnce();
  });

  it('sem onClick: não renderiza como botão', () => {
    render(<StatCard titulo="Total" valor={10} icone={PeopleIcon} />);

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
