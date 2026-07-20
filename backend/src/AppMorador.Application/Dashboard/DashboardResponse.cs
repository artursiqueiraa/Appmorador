using AppMorador.Domain.Entities;

namespace AppMorador.Application.Dashboard;

/// <summary>
/// Contrato do dashboard exposto ao app. Nunca contem termos tecnicos (Contact ID,
/// numero de zona, DVR, ISAPI, ONVIF) — so linguagem que o usuario final entende.
/// </summary>
public sealed class DashboardResponse
{
    public required string Nome { get; init; }

    public required TipoPropriedade Tipo { get; init; }

    public required string StatusSeguranca { get; init; }

    public required int PontuacaoSaude { get; init; }

    public string? UltimoEvento { get; init; }

    public DateTime? UltimoEventoEmUtc { get; init; }

    public required int QuantidadeCentrais { get; init; }

    public required int QuantidadeGravadores { get; init; }

    public required int QuantidadeCameras { get; init; }

    public required int QuantidadeSensores { get; init; }

    public required int QuantidadePessoas { get; init; }
}
