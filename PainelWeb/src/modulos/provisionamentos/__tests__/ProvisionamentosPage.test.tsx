import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../../testUtils/renderWithProviders';
import { GlobalToast } from '../../../components/GlobalToast';
import { ProvisionamentosPage } from '../ProvisionamentosPage';
import { provisionamentosAdaptador } from '../adaptadores/provisionamentosAdaptador';
import type { Vinculo } from '../types';

vi.mock('../adaptadores/provisionamentosAdaptador', () => ({
  provisionamentosAdaptador: {
    listarAtivos: vi.fn(),
    obterDashboard: vi.fn(),
    listarHistorico: vi.fn(),
    provisionar: vi.fn(),
    trocar: vi.fn(),
    desvincular: vi.fn(),
    listarEquipamentosParaAlocacao: vi.fn(),
  },
}));

const VINCULO: Vinculo = {
  id: 'vinculo-1',
  equipamentoId: 'equip-1',
  equipamentoNome: 'Central JFL',
  propriedadeId: 'prop-1',
  propriedadeNome: 'Casa Teste',
  dataInicioUtc: '2026-07-01T00:00:00Z',
  dataFimUtc: null,
  ativo: true,
  criadoPorUsuarioId: 'user-1',
  observacoes: null,
};

describe('ProvisionamentosPage', () => {
  beforeEach(() => {
    vi.mocked(provisionamentosAdaptador.listarAtivos).mockResolvedValue({
      itens: [VINCULO],
      paginaAtual: 1,
      totalPaginas: 1,
      totalItens: 1,
    });
    vi.mocked(provisionamentosAdaptador.obterDashboard).mockResolvedValue({
      totalEquipamentos: 10,
      totalProvisionados: 4,
      totalDisponiveis: 6,
    });
  });

  it('renderiza os cards do dashboard e a tabela de vínculos ativos', async () => {
    renderWithProviders(<ProvisionamentosPage />);

    expect(await screen.findByText('Central JFL')).toBeInTheDocument();
    expect(screen.getByText('10')).toBeInTheDocument();
    expect(screen.getByText('4')).toBeInTheDocument();
    expect(screen.getByText('6')).toBeInTheDocument();
  });

  it('abre o diálogo de desvincular e chama a mutation ao confirmar', async () => {
    vi.mocked(provisionamentosAdaptador.desvincular).mockResolvedValue({} as never);
    renderWithProviders(<ProvisionamentosPage />);
    await screen.findByText('Central JFL');

    fireEvent.click(screen.getByTitle('Desvincular'));
    expect(await screen.findByText('Desvincular equipamento')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Confirmar'));

    await waitFor(() => {
      expect(provisionamentosAdaptador.desvincular).toHaveBeenCalledWith('equip-1');
    });
  });

  it('erro ao desvincular é mostrado como mensagem amigável', async () => {
    vi.mocked(provisionamentosAdaptador.desvincular).mockRejectedValue({
      isAxiosError: true,
      response: { data: { error: 'Este equipamento não está provisionado em nenhuma propriedade.' } },
    });
    renderWithProviders(
      <>
        <ProvisionamentosPage />
        <GlobalToast />
      </>,
    );
    await screen.findByText('Central JFL');

    fireEvent.click(screen.getByTitle('Desvincular'));
    await screen.findByText('Desvincular equipamento');
    fireEvent.click(screen.getByText('Confirmar'));

    expect(await screen.findByText('Este equipamento não está provisionado em nenhuma propriedade.')).toBeInTheDocument();
  });

  it('histórico: abre o drawer e mostra o ciclo de vida (vínculo encerrado + observações)', async () => {
    vi.mocked(provisionamentosAdaptador.listarHistorico).mockResolvedValue([
      { ...VINCULO, id: 'v-antigo', dataFimUtc: '2026-07-15T00:00:00Z', ativo: false, observacoes: 'instalação inicial' },
      VINCULO,
    ]);
    renderWithProviders(<ProvisionamentosPage />);
    await screen.findByText('Central JFL');

    fireEvent.click(screen.getByTitle('Histórico'));

    expect(await screen.findByText(/instalação inicial/)).toBeInTheDocument();
    expect(provisionamentosAdaptador.listarHistorico).toHaveBeenCalledWith('equip-1');
  });

  it('wizard de provisionar abre com os campos de propriedade e equipamento', async () => {
    vi.mocked(provisionamentosAdaptador.listarEquipamentosParaAlocacao).mockResolvedValue([]);
    renderWithProviders(<ProvisionamentosPage />);
    await screen.findByText('Central JFL');

    fireEvent.click(screen.getByText('Provisionar Equipamento'));

    expect(await screen.findByText('Provisionar Equipamento', { selector: 'h2' })).toBeInTheDocument();
    expect(screen.getByLabelText('Buscar cliente por nome ou e-mail')).toBeInTheDocument();
    expect(screen.getByText('Equipamento disponível')).toBeInTheDocument();
    // NOTA: a seleção real via o Autocomplete (MUI + Popper.js) não é exercitada aqui pelo mesmo
    // motivo documentado em EquipamentosListPage.test.tsx — limitação de jsdom, não do produto.
  });
});
