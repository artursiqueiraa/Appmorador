import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../../testUtils/renderWithProviders';
import { DiagnosticoEquipamentosPage } from '../DiagnosticoEquipamentosPage';
import { diagnosticoAdaptador } from '../adaptadores/diagnosticoAdaptador';
import type { DiagnosticoEquipamento } from '../types';

vi.mock('../adaptadores/diagnosticoAdaptador', () => ({
  diagnosticoAdaptador: {
    obterStatusEquipamentos: vi.fn(),
  },
}));

const EQUIPAMENTO: DiagnosticoEquipamento = {
  equipamentoId: 'equip-1',
  equipamentoNome: 'Central JFL',
  fabricante: 'Jfl',
  propriedadeId: 'prop-1',
  propriedadeNome: 'Casa Teste',
  status: 'Online',
  estadoOperacional: 'Ativo',
  ultimoPingUtc: '2026-07-28T10:00:00Z',
  temProblemaAtivo: false,
  quantidadeEventosRecentes: 3,
  ultimoEventoDescricao: 'Restauração de zona',
  ultimoEventoEmUtc: '2026-07-28T09:00:00Z',
};

describe('DiagnosticoEquipamentosPage', () => {
  beforeEach(() => {
    vi.mocked(diagnosticoAdaptador.obterStatusEquipamentos).mockResolvedValue({
      itens: [EQUIPAMENTO],
      paginaAtual: 1,
      totalPaginas: 1,
      totalItens: 1,
    });
  });

  it('renderiza o status agregado dos equipamentos', async () => {
    renderWithProviders(<DiagnosticoEquipamentosPage />);

    expect(await screen.findByText('Central JFL')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('clicar numa linha abre o drawer de detalhe com as ações de hardware desabilitadas', async () => {
    renderWithProviders(<DiagnosticoEquipamentosPage />);
    await screen.findByText('Central JFL');

    fireEvent.click(screen.getByText('Central JFL'));

    await waitFor(() => {
      expect(screen.getByText(/Restauração de zona/)).toBeInTheDocument();
    });
    expect(screen.getByText('Sincronizar').closest('button')).toBeDisabled();
    expect(screen.getByText('Reiniciar').closest('button')).toBeDisabled();
  });

  it('polling: padrão do app é "A cada 30s" (não desligado) já na primeira carga', async () => {
    renderWithProviders(<DiagnosticoEquipamentosPage />);
    await screen.findByText('Central JFL');

    expect(screen.getByText('A cada 30s')).toBeInTheDocument();
  });

  it('polling: trocar para "Desligado" atualiza o seletor (para a atualização automática)', async () => {
    renderWithProviders(<DiagnosticoEquipamentosPage />);
    await screen.findByText('Central JFL');

    const seletor = screen.getByLabelText('Atualização automática');
    fireEvent.mouseDown(seletor);
    fireEvent.click(await screen.findByText('Desligado'));

    await waitFor(() => {
      expect(screen.getByText('Desligado')).toBeInTheDocument();
    });
  });
});
