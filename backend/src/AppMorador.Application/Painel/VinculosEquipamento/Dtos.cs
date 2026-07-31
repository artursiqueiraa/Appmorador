namespace AppMorador.Application.Painel.VinculosEquipamento;

/// <summary>
/// Sprint 22B (ADR 0031) — DTOs do vínculo Equipamento↔Propriedade. Namespace deliberadamente
/// diferente de `AppMorador.Application.Provisionamentos` (ADR 0028, Sprint 21) — mesma palavra
/// "Provisionamento" na linguagem de negócio desta Sprint, mas é uma entidade nova
/// (`VinculoEquipamentoPropriedade`), nunca confundir com o `Provisionamento` já existente.
/// </summary>
public sealed class VinculoResponse
{
    public required Guid Id { get; init; }

    public required Guid EquipamentoId { get; init; }

    public string? EquipamentoNome { get; init; }

    public required Guid PropriedadeId { get; init; }

    public string? PropriedadeNome { get; init; }

    public required DateTime DataInicioUtc { get; init; }

    public DateTime? DataFimUtc { get; init; }

    /// <summary>Derivado de `DataFimUtc == null` — nunca um campo próprio persistido (ver entidade).</summary>
    public required bool Ativo { get; init; }

    public required Guid CriadoPorUsuarioId { get; init; }

    public string? Observacoes { get; init; }
}

public sealed class VinculosPaginadosResponse
{
    public required IReadOnlyList<VinculoResponse> Itens { get; init; }

    public required int PaginaAtual { get; init; }

    public required int TotalPaginas { get; init; }

    public required int TotalItens { get; init; }
}

public sealed class ProvisionarEquipamentoRequest
{
    public required Guid EquipamentoId { get; init; }

    public required Guid PropriedadeId { get; init; }

    public string? Observacoes { get; init; }
}

public sealed class TrocarEquipamentoRequest
{
    public required Guid PropriedadeId { get; init; }

    public required Guid EquipamentoAntigoId { get; init; }

    public required Guid EquipamentoNovoId { get; init; }

    public string? Observacoes { get; init; }
}

public sealed class DashboardAlocacaoResponse
{
    public required int TotalEquipamentos { get; init; }

    public required int TotalProvisionados { get; init; }

    public required int TotalDisponiveis { get; init; }
}
