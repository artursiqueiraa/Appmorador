using AppMorador.Application.Common;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Propriedades;

/// <summary>Sprint 21 (ADR 0026) — o que a propriedade contratou. Gerenciado por Técnico/Master (é decisão comercial/de instalação, não do cliente).</summary>
public interface IPropriedadeFeatureFlagServico
{
    Task<Result<IReadOnlyList<FeatureFlag>>> ListarAtivasAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<FeatureFlag>>> DefinirAsync(Guid propriedadeId, FeatureFlag feature, bool ativo, CancellationToken cancellationToken);
}
