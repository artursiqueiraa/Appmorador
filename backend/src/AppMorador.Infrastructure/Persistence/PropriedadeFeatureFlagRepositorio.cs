using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class PropriedadeFeatureFlagRepositorio : IPropriedadeFeatureFlagRepositorio
{
    private readonly AppDbContext _db;

    public PropriedadeFeatureFlagRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FeatureFlag>> ListAtivasAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.PropriedadesFeatureFlag
            .Where(f => f.PropriedadeId == propriedadeId && f.Ativo)
            .Select(f => f.Feature)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> TemFeatureAtivaAsync(Guid propriedadeId, FeatureFlag feature, CancellationToken cancellationToken) =>
        _db.PropriedadesFeatureFlag.AnyAsync(f => f.PropriedadeId == propriedadeId && f.Feature == feature && f.Ativo, cancellationToken);

    public async Task DefinirAsync(Guid propriedadeId, FeatureFlag feature, bool ativo, CancellationToken cancellationToken)
    {
        var existente = await _db.PropriedadesFeatureFlag
            .FirstOrDefaultAsync(f => f.PropriedadeId == propriedadeId && f.Feature == feature, cancellationToken)
            .ConfigureAwait(false);

        if (existente is not null)
        {
            existente.Ativo = ativo;
            return;
        }

        await _db.PropriedadesFeatureFlag.AddAsync(new PropriedadeFeatureFlag
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Feature = feature,
            Ativo = ativo,
            AtivadoEmUtc = DateTime.UtcNow,
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
