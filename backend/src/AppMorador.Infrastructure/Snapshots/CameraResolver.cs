using AppMorador.Domain.Entities;
using AppMorador.Domain.Snapshots;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Snapshots;

/// <summary>
/// Unica classe que sabe consultar VinculoZonaCamera/Camera/Gravador. Extraida de
/// SnapshotCaptureService, que antes fazia essa consulta diretamente — agora so
/// recebe o resultado ja resolvido e executa a captura.
/// </summary>
internal sealed class CameraResolver : ICameraResolver
{
    private readonly AppDbContext _db;

    public CameraResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Camera?> ResolveAsync(Guid zonaId, CancellationToken cancellationToken)
    {
        var link = await _db.VinculosZonaCamera
            .Include(l => l.Camera)
            .ThenInclude(c => c!.Gravador)
            .FirstOrDefaultAsync(l => l.ZonaId == zonaId, cancellationToken)
            .ConfigureAwait(false);

        return link?.Camera;
    }
}
