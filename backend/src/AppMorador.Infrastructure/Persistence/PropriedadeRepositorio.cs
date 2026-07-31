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

    public async Task<IReadOnlyDictionary<TipoPropriedade, int>> ContarPorTipoAsync(CancellationToken cancellationToken)
    {
        var grupos = await _db.Propriedades
            .AsNoTracking()
            .GroupBy(p => p.Tipo)
            .Select(g => new { Tipo = g.Key, Quantidade = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return grupos.ToDictionary(g => g.Tipo, g => g.Quantidade);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> ContarPorProprietariosAsync(
        IReadOnlyCollection<Guid> proprietarioIds, CancellationToken cancellationToken)
    {
        var grupos = await _db.Propriedades
            .AsNoTracking()
            .Where(p => proprietarioIds.Contains(p.ProprietarioId))
            .GroupBy(p => p.ProprietarioId)
            .Select(g => new { ProprietarioId = g.Key, Quantidade = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return grupos.ToDictionary(g => g.ProprietarioId, g => g.Quantidade);
    }

    public async Task AddAsync(Propriedade propriedade, CancellationToken cancellationToken) =>
        await _db.Propriedades.AddAsync(propriedade, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
