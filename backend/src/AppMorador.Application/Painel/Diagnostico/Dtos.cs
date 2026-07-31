using AppMorador.Domain.Entities;

namespace AppMorador.Application.Painel.Diagnostico;

public sealed class DiagnosticoEquipamentoResponse
{
    public required Guid EquipamentoId { get; init; }

    public required string EquipamentoNome { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string PropriedadeNome { get; init; }

    public required StatusEquipamento Status { get; init; }

    public required EstadoOperacionalEquipamento EstadoOperacional { get; init; }

    /// <summary>Momento da última comunicação conhecida — o mais recente entre a sincronização do equipamento e o último status de central capturado.</summary>
    public DateTime? UltimoPingUtc { get; init; }

    /// <summary>Só populado para centrais JFL (StatusCentralJfl) — null para outros fabricantes.</summary>
    public bool? TemProblemaAtivo { get; init; }

    public required int QuantidadeEventosRecentes { get; init; }

    public string? UltimoEventoDescricao { get; init; }

    public DateTime? UltimoEventoEmUtc { get; init; }
}

public sealed class DiagnosticoEquipamentosPaginadosResponse
{
    public required IReadOnlyList<DiagnosticoEquipamentoResponse> Itens { get; init; }

    public required int PaginaAtual { get; init; }

    public required int TotalPaginas { get; init; }

    public required int TotalItens { get; init; }
}
