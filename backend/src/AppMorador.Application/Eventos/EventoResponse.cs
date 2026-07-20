namespace AppMorador.Application.Eventos;

/// <summary>
/// DTO exposto pela Api — linguagem do produto, não a taxonomia interna. Nunca inclui
/// <see cref="OrigemEvento"/>/<see cref="CategoriaEvento"/>/<see cref="SeveridadeEvento"/>
/// diretamente, para manter liberdade de evoluir a classificação interna sem quebrar o
/// contrato público.
/// </summary>
public sealed class EventoResponse
{
    public required Guid Id { get; init; }

    public required string Titulo { get; init; }

    public string? Descricao { get; init; }

    public required DateTime OcorridoEmUtc { get; init; }

    /// <summary>Único sinal de ênfase visual exposto — verdadeiro quando o evento é crítico internamente.</summary>
    public required bool Destaque { get; init; }
}