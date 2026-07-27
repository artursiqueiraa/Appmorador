using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class CameraRepositorio : ICameraRepositorio
{
    private readonly AppDbContext _db;

    public CameraRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Camera?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Cameras
            .Include(c => c.Propriedade)
            .Include(c => c.Gravador)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Camera>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Cameras
            .Where(c => c.PropriedadeId == propriedadeId)
            .OrderBy(c => c.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
