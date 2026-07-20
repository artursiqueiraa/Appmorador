using AppMorador.Application.Dashboard;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Dashboard;

/// <summary>
/// Unica classe que sabe cruzar Central/Zona/VinculoZonaCamera/Camera/Ocorrencia por
/// PropriedadeId — DashboardServico (Application) so consome o resultado ja pronto.
/// </summary>
internal sealed class ConsultaDashboardServico : IConsultaDashboardServico
{
    private readonly AppDbContext _db;

    public ConsultaDashboardServico(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DadosBrutosDashboard> GetRawDataAsync(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var centralIds = await _db.Centrais
            .Where(c => c.PropriedadeId == propriedadeId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalZonas = centralIds.Count == 0
            ? 0
            : await _db.Zonas.CountAsync(z => centralIds.Contains(z.CentralId), cancellationToken).ConfigureAwait(false);

        var zonasComCamera = centralIds.Count == 0
            ? 0
            : await _db.VinculosZonaCamera
                .Where(l => _db.Zonas.Any(z => z.Id == l.ZonaId && centralIds.Contains(z.CentralId)))
                .Select(l => l.ZonaId)
                .Distinct()
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

        var quantidadeCameras = await _db.Cameras
            .CountAsync(c => c.PropriedadeId == propriedadeId, cancellationToken)
            .ConfigureAwait(false);

        var quantidadeGravadores = await _db.Gravadores
            .CountAsync(g => g.PropriedadeId == propriedadeId, cancellationToken)
            .ConfigureAwait(false);

        var ultimaOcorrencia = await _db.Ocorrencias
            .Where(o => o.PropriedadeId == propriedadeId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new
            {
                o.CreatedAtUtc,
                o.CodigoEvento,
                NomeZona = o.Zona != null ? o.Zona.Nome : null,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new DadosBrutosDashboard
        {
            TemCentral = centralIds.Count > 0,
            QuantidadeCentrais = centralIds.Count,
            QuantidadeGravadores = quantidadeGravadores,
            TotalZonas = totalZonas,
            ZonasComCamera = zonasComCamera,
            QuantidadeCameras = quantidadeCameras,
            UltimaOcorrenciaEmUtc = ultimaOcorrencia?.CreatedAtUtc,
            UltimaOcorrenciaCodigoContactId = ultimaOcorrencia?.CodigoEvento,
            UltimaOcorrenciaNomeZona = ultimaOcorrencia?.NomeZona,
        };
    }
}
