using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Propriedades;

public sealed class PropriedadeFeatureFlagServico : IPropriedadeFeatureFlagServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IPropriedadeFeatureFlagRepositorio _features;

    public PropriedadeFeatureFlagServico(IPropriedadeRepositorio propriedades, IPropriedadeFeatureFlagRepositorio features)
    {
        _propriedades = propriedades;
        _features = features;
    }

    public async Task<Result<IReadOnlyList<FeatureFlag>>> ListarAtivasAsync(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<IReadOnlyList<FeatureFlag>>.Fail("Propriedade não encontrada.");
        }

        return Result<IReadOnlyList<FeatureFlag>>.Ok(await _features.ListAtivasAsync(propriedadeId, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<IReadOnlyList<FeatureFlag>>> DefinirAsync(Guid propriedadeId, FeatureFlag feature, bool ativo, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<IReadOnlyList<FeatureFlag>>.Fail("Propriedade não encontrada.");
        }

        await _features.DefinirAsync(propriedadeId, feature, ativo, cancellationToken).ConfigureAwait(false);
        await _features.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<FeatureFlag>>.Ok(await _features.ListAtivasAsync(propriedadeId, cancellationToken).ConfigureAwait(false));
    }
}
