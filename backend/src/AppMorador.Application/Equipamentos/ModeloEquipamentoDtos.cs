using AppMorador.Domain.Entities;

namespace AppMorador.Application.Equipamentos;

public sealed class CriarModeloEquipamentoRequest
{
    public required FabricanteEquipamento Fabricante { get; init; }

    public required string Nome { get; init; }
}

public sealed class DefinirCapacidadesRequest
{
    public required IReadOnlyCollection<EquipamentoCapacidade> Capacidades { get; init; }
}

public sealed class ModeloEquipamentoResponse
{
    public Guid Id { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public required string Nome { get; init; }

    public IReadOnlyList<EquipamentoCapacidade> Capacidades { get; init; } = [];
}
