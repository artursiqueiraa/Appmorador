namespace AppMorador.Application.Dashboard;

/// <summary>
/// Porta de leitura para os dados brutos do dashboard — cruza tabelas que pertencem
/// ao modulo de alarme/camera (Central, Zona, VinculoZonaCamera, Camera, Ocorrencia),
/// fora do agregado Propriedade. Implementacao (EF Core) fica em Infrastructure;
/// DashboardServico so formata o que esta porta devolve.
/// </summary>
public interface IConsultaDashboardServico
{
    Task<DadosBrutosDashboard> GetRawDataAsync(Guid propriedadeId, CancellationToken cancellationToken);
}

public sealed class DadosBrutosDashboard
{
    public required bool TemCentral { get; init; }

    public required int QuantidadeCentrais { get; init; }

    public required int QuantidadeGravadores { get; init; }

    public required int TotalZonas { get; init; }

    public required int ZonasComCamera { get; init; }

    public required int QuantidadeCameras { get; init; }

    public DateTime? UltimaOcorrenciaEmUtc { get; init; }

    public string? UltimaOcorrenciaCodigoContactId { get; init; }

    public string? UltimaOcorrenciaNomeZona { get; init; }
}
