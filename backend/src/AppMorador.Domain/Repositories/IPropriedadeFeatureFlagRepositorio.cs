using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

public interface IPropriedadeFeatureFlagRepositorio
{
    Task<IReadOnlyList<FeatureFlag>> ListAtivasAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<bool> TemFeatureAtivaAsync(Guid propriedadeId, FeatureFlag feature, CancellationToken cancellationToken);

    /// <summary>Upsert de uma única flag — cria a linha se não existir, ou só ajusta Ativo se já existir (nunca duplica).</summary>
    Task DefinirAsync(Guid propriedadeId, FeatureFlag feature, bool ativo, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
