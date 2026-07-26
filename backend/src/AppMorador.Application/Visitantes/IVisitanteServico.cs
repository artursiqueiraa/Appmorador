using AppMorador.Application.Common;

namespace AppMorador.Application.Visitantes;

public interface IVisitanteServico
{
    Task<Result<VisitanteResponse>> CreateAsync(Guid proprietarioId, Guid propriedadeId, CriarVisitanteRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VisitanteResponse>>> ListByPropriedadeAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<VisitanteResponse>> UpdateAsync(Guid proprietarioId, Guid visitanteId, AtualizarVisitanteRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid visitanteId, CancellationToken cancellationToken);
}
