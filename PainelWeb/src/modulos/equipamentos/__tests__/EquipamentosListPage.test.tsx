import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../../testUtils/renderWithProviders';
import { GlobalToast } from '../../../components/GlobalToast';
import { EquipamentosListPage } from '../EquipamentosListPage';
import { equipamentosAdaptador } from '../adaptadores/equipamentosAdaptador';
import type { EquipamentoAdmin } from '../types';

vi.mock('../adaptadores/equipamentosAdaptador', () => ({
  equipamentosAdaptador: {
    listar: vi.fn(),
    criar: vi.fn(),
    atualizar: vi.fn(),
    excluir: vi.fn(),
  },
}));

vi.mock('../../../services/proprietariosService', () => ({
  proprietariosService: {
    listar: vi.fn(),
    obterDetalhe: vi.fn(),
  },
}));

const EQUIPAMENTO: EquipamentoAdmin = {
  id: 'equip-1',
  propriedadeId: 'prop-1',
  propriedadeNome: 'Casa Teste',
  nome: 'Central JFL',
  fabricante: 'Jfl',
  modelo: 'Active 100',
  numeroSerie: '12345',
  status: 'Online',
  estadoOperacional: 'Ativo',
  createdAtUtc: '2026-07-01T00:00:00Z',
  excluido: false,
};

function clicarEditar() {
  const [botaoEditar] = screen.getAllByRole('button').filter((b) => b.querySelector('svg[data-testid="EditIcon"]'));
  fireEvent.click(botaoEditar);
}

describe('EquipamentosListPage', () => {
  beforeEach(() => {
    vi.mocked(equipamentosAdaptador.listar).mockResolvedValue({
      itens: [EQUIPAMENTO],
      paginaAtual: 1,
      totalPaginas: 1,
      totalItens: 1,
    });
  });

  it('renderiza a lista de equipamentos vinda do backend', async () => {
    renderWithProviders(<EquipamentosListPage />);

    expect(await screen.findByText('Central JFL')).toBeInTheDocument();
    expect(screen.getByText('12345')).toBeInTheDocument();
  });

  it('abre o formulário de cadastro ao clicar em "Novo Equipamento"', async () => {
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central JFL');

    fireEvent.click(screen.getByText('Novo Equipamento'));

    expect(await screen.findByText('Novo Equipamento', { selector: 'h2' })).toBeInTheDocument();
  });

  it('clicar numa linha abre o drawer de detalhes (somente leitura)', async () => {
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central JFL');

    fireEvent.click(screen.getByText('Central JFL'));

    await waitFor(() => {
      expect(screen.getByText('Editar cadastro')).toBeInTheDocument();
    });
    expect(screen.getAllByText('12345')).toHaveLength(2);
    expect(screen.queryByText('Editar Equipamento')).not.toBeInTheDocument();
  });

  it('clicar no ícone de editar abre o formulário em modo edição', async () => {
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central JFL');

    clicarEditar();

    await waitFor(() => {
      expect(screen.getByText('Editar Equipamento')).toBeInTheDocument();
    });
  });

  it('busca dispara uma nova consulta com o termo informado', async () => {
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central JFL');

    fireEvent.change(screen.getByPlaceholderText('Buscar por nome ou número de série'), {
      target: { value: 'JFL' },
    });

    await waitFor(
      () => {
        expect(equipamentosAdaptador.listar).toHaveBeenCalledWith(
          1,
          20,
          expect.objectContaining({ busca: 'JFL' }),
        );
      },
      { timeout: 1000 },
    );
  });

  it('paginação: trocar de página dispara nova consulta com a página correta', async () => {
    vi.mocked(equipamentosAdaptador.listar).mockResolvedValue({
      itens: [EQUIPAMENTO],
      paginaAtual: 1,
      totalPaginas: 3,
      totalItens: 41,
    });
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central JFL');

    fireEvent.click(screen.getByText('2'));

    await waitFor(() => {
      expect(equipamentosAdaptador.listar).toHaveBeenCalledWith(2, 20, expect.anything());
    });
  });

  it('editar equipamento: alterar nome e salvar chama atualizar com o payload correto', async () => {
    vi.mocked(equipamentosAdaptador.atualizar).mockResolvedValue({ ...EQUIPAMENTO, nome: 'Central Editada' });
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central JFL');

    clicarEditar();
    await screen.findByText('Editar Equipamento');

    const campoNome = screen.getByDisplayValue('Central JFL');
    fireEvent.change(campoNome, { target: { value: 'Central Editada' } });
    fireEvent.click(screen.getByText('Salvar'));

    await waitFor(() => {
      expect(equipamentosAdaptador.atualizar).toHaveBeenCalledWith(
        'equip-1',
        expect.objectContaining({ nome: 'Central Editada', numeroSerie: '12345' }),
      );
    });
  });

  // NOTA: o fluxo completo de Cadastro (selecionar cliente/propriedade via o Autocomplete de
  // `SeletorPropriedade`) não é exercitado por um teste automatizado aqui — a combinação MUI
  // Autocomplete + Popper.js trava o jsdom deste ambiente (`getScrollParent`/`isScrollParent`
  // chamando `getComputedStyle` gera `TypeError: object null is not iterable`), uma limitação
  // conhecida de ambiente, não um defeito no componente. O teste "editar equipamento" acima
  // já prova ponta a ponta o mesmo código de submissão (`salvar()` → mutation → payload
  // correto) que o Cadastro compartilha, só sem passar pelo Autocomplete. Documentado em
  // `docs/testing/Sprint22B.md` como limitação, não como item ignorado silenciosamente.

  it('excluir equipamento: confirmar diálogo chama excluir e mostra sucesso', async () => {
    vi.mocked(equipamentosAdaptador.excluir).mockResolvedValue({} as never);
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central JFL');

    const [botaoExcluir] = screen.getAllByRole('button').filter((b) => b.querySelector('svg[data-testid="DeleteIcon"]'));
    fireEvent.click(botaoExcluir);

    expect(await screen.findByText('Excluir equipamento')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Confirmar'));

    await waitFor(() => {
      expect(equipamentosAdaptador.excluir).toHaveBeenCalledWith('equip-1');
    });
  });

  it('editar equipamento Control iD: mostra IP/Porta/Usuário/Senha, não Número de Série', async () => {
    vi.mocked(equipamentosAdaptador.listar).mockResolvedValue({
      itens: [
        {
          ...EQUIPAMENTO,
          id: 'equip-2',
          fabricante: 'ControlId',
          nome: 'Catraca Recepção',
          numeroSerie: 'CTID-999',
          ip: '192.168.1.50',
          porta: 80,
          usuario: 'admin',
        },
      ],
      paginaAtual: 1,
      totalPaginas: 1,
      totalItens: 1,
    });
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Catraca Recepção');

    clicarEditar();
    await screen.findByText('Editar Equipamento');

    expect(screen.getByDisplayValue('192.168.1.50')).toBeInTheDocument();
    expect(screen.getByDisplayValue('80')).toBeInTheDocument();
    expect(screen.getByDisplayValue('admin')).toBeInTheDocument();
    expect(screen.getByText('Deixe em branco para manter a senha atual')).toBeInTheDocument();
    expect(screen.queryByLabelText('Número de Série')).not.toBeInTheDocument();
  });

  it('editar equipamento Intelbras: mostra IP/Porta/Senha, mas nunca Usuário', async () => {
    vi.mocked(equipamentosAdaptador.listar).mockResolvedValue({
      itens: [
        {
          ...EQUIPAMENTO,
          id: 'equip-3',
          fabricante: 'Intelbras',
          nome: 'Central Intelbras',
          numeroSerie: null,
          ip: '192.168.1.60',
          porta: 9009,
        },
      ],
      paginaAtual: 1,
      totalPaginas: 1,
      totalItens: 1,
    });
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central Intelbras');

    clicarEditar();
    await screen.findByText('Editar Equipamento');

    expect(screen.getByDisplayValue('192.168.1.60')).toBeInTheDocument();
    expect(screen.getByDisplayValue('9009')).toBeInTheDocument();
    expect(screen.queryByLabelText('Usuário')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Número de Série')).not.toBeInTheDocument();
  });

  it('editar equipamento JFL: mostra só Número de Série, nunca IP/Porta/Usuário/Senha', async () => {
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Central JFL');

    clicarEditar();
    await screen.findByText('Editar Equipamento');

    expect(screen.getByDisplayValue('12345')).toBeInTheDocument();
    expect(screen.queryByLabelText('Endereço IP')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Usuário')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Senha')).not.toBeInTheDocument();
  });

  it('drawer de detalhes: equipamento Control iD mostra informações descobertas pelo Provider', async () => {
    vi.mocked(equipamentosAdaptador.listar).mockResolvedValue({
      itens: [
        {
          ...EQUIPAMENTO,
          id: 'equip-4',
          fabricante: 'ControlId',
          nome: 'Catraca com Descoberta',
          numeroSerie: 'CTID-777',
          ip: '192.168.1.70',
          informacoesDescobertas: { Versao: '2.14', NomeDispositivo: 'iDFace' },
        },
      ],
      paginaAtual: 1,
      totalPaginas: 1,
      totalItens: 1,
    });
    renderWithProviders(<EquipamentosListPage />);
    await screen.findByText('Catraca com Descoberta');

    fireEvent.click(screen.getByText('Catraca com Descoberta'));

    await waitFor(() => {
      expect(screen.getByText('2.14')).toBeInTheDocument();
    });
    expect(screen.getByText('iDFace')).toBeInTheDocument();
  });

  it('erro 409 ao salvar é mostrado como mensagem amigável, não stack trace', async () => {
    vi.mocked(equipamentosAdaptador.atualizar).mockRejectedValue({
      isAxiosError: true,
      response: { data: { error: 'Já existe um equipamento com este Número de Série nesta propriedade.' } },
    });
    renderWithProviders(
      <>
        <EquipamentosListPage />
        <GlobalToast />
      </>,
    );
    await screen.findByText('Central JFL');

    clicarEditar();
    await screen.findByText('Editar Equipamento');
    fireEvent.click(screen.getByText('Salvar'));

    expect(await screen.findByText('Já existe um equipamento com este Número de Série nesta propriedade.')).toBeInTheDocument();
  });
});
