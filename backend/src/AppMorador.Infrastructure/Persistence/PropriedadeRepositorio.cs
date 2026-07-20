using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class PropriedadeRepositorio : IPropriedadeRepositorio
{
    private readonly AppDbContext _db;

    public PropriedadeRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Propriedade?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Propriedades.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Propriedade>> ListByOwnerAsync(Guid proprietarioId, CancellationToken cancellationToken) =>
        await _db.Propriedades.AsNoTracking().Where(p => p.ProprietarioId == proprietarioId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(Propriedade propriedade, CancellationToken cancellationToken) =>
        await _db.Propriedades.AddAsync(propriedade, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
