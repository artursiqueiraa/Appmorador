namespace AppMorador.Application.VinculosVeiculoVaga;

public sealed class VincularVeiculoVagaRequest
{
    public required Guid VagaId { get; set; }
}

public sealed class VinculoVeiculoVagaResponse
{
    public required Guid Id { get; init; }

    public required Guid VeiculoId { get; init; }

    public required Guid VagaId { get; init; }

    public required string VagaNumero { get; init; }

    public required DateTime DataInicioUtc { get; init; }

    public DateTime? DataFimUtc { get; init; }
}
