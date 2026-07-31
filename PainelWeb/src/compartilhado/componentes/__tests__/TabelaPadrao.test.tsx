import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import DevicesIcon from '@mui/icons-material/Devices';
import { TabelaPadrao, type ColunaTabela } from '../TabelaPadrao';

interface ItemTeste {
  id: string;
  nome: string;
}

const COLUNAS: ColunaTabela<ItemTeste>[] = [{ cabecalho: 'Nome', render: (item) => item.nome }];
const VAZIO = { icone: DevicesIcon, titulo: 'Nada aqui', descricao: 'Descrição do vazio' };

describe('TabelaPadrao', () => {
  it('carregando: mostra skeleton, não a tabela nem o vazio', () => {
    render(<TabelaPadrao colunas={COLUNAS} itens={[]} chave={(i) => i.id} carregando vazio={VAZIO} />);

    expect(screen.queryByRole('table')).not.toBeInTheDocument();
    expect(screen.queryByText('Nada aqui')).not.toBeInTheDocument();
  });

  it('sem itens: mostra o estado vazio', () => {
    render(<TabelaPadrao colunas={COLUNAS} itens={[]} chave={(i) => i.id} vazio={VAZIO} />);

    expect(screen.getByText('Nada aqui')).toBeInTheDocument();
  });

  it('com itens: renderiza uma linha por item', () => {
    const itens = [{ id: '1', nome: 'Item A' }, { id: '2', nome: 'Item B' }];
    render(<TabelaPadrao colunas={COLUNAS} itens={itens} chave={(i) => i.id} vazio={VAZIO} />);

    expect(screen.getByText('Item A')).toBeInTheDocument();
    expect(screen.getByText('Item B')).toBeInTheDocument();
  });

  it('onRowClick: chama o handler com o item clicado', () => {
    const onRowClick = vi.fn();
    const itens = [{ id: '1', nome: 'Item A' }];
    render(<TabelaPadrao colunas={COLUNAS} itens={itens} chave={(i) => i.id} onRowClick={onRowClick} vazio={VAZIO} />);

    fireEvent.click(screen.getByText('Item A'));

    expect(onRowClick).toHaveBeenCalledWith(itens[0]);
  });
});
