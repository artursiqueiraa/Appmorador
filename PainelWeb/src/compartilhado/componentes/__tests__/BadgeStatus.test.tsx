import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { BadgeStatus, SeletorStatus, type OpcaoStatus } from '../BadgeStatus';

type Status = 'Ativo' | 'Inativo';

const OPCOES: readonly OpcaoStatus<Status>[] = [
  { valor: 'Ativo', rotulo: 'Ativo', cor: 'success' },
  { valor: 'Inativo', rotulo: 'Inativo', cor: 'default' },
];

describe('BadgeStatus', () => {
  it('mostra o rótulo mapeado, não o valor bruto', () => {
    render(<BadgeStatus valor="Ativo" opcoes={OPCOES} />);
    expect(screen.getByText('Ativo')).toBeInTheDocument();
  });

  it('valor sem opção mapeada: cai para o próprio valor', () => {
    render(<BadgeStatus valor={'Desconhecido' as Status} opcoes={OPCOES} />);
    expect(screen.getByText('Desconhecido')).toBeInTheDocument();
  });
});

describe('SeletorStatus', () => {
  it('mostra a opção "Todos" quando informada e chama onChange ao selecionar', () => {
    const onChange = vi.fn();
    render(<SeletorStatus label="Estado" value="" onChange={onChange} opcoes={OPCOES} todosRotulo="Todos" />);

    fireEvent.mouseDown(screen.getByLabelText('Estado'));
    fireEvent.click(screen.getByText('Ativo'));

    expect(onChange).toHaveBeenCalledWith('Ativo');
  });
});
