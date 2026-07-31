import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import PeopleIcon from '@mui/icons-material/People';
import { EmptyState } from '../EmptyState';

describe('EmptyState', () => {
  it('renderiza título e descrição', () => {
    render(<EmptyState icone={PeopleIcon} titulo="Nenhum cliente" descricao="Cadastre o primeiro." />);

    expect(screen.getByText('Nenhum cliente')).toBeInTheDocument();
    expect(screen.getByText('Cadastre o primeiro.')).toBeInTheDocument();
  });

  it('sem `acao`: não renderiza nenhum botão', () => {
    render(<EmptyState icone={PeopleIcon} titulo="Nenhum cliente" descricao="Cadastre o primeiro." />);

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('com `acao`: renderiza o botão e chama onClick', () => {
    const onClick = vi.fn();
    render(<EmptyState icone={PeopleIcon} titulo="Vazio" descricao="Desc" acao={{ rotulo: 'Adicionar', onClick }} />);

    fireEvent.click(screen.getByRole('button', { name: 'Adicionar' }));

    expect(onClick).toHaveBeenCalledOnce();
  });
});
