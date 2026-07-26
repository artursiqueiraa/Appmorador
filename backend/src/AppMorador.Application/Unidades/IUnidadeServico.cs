using AppMorador.Application.Common;

namespace AppMorador.Application.Unidades;

public interface IUnidadeServico
{
    Task<Result<UnidadeResponse>> CreateAsync(Guid proprietarioId, Guid propriedadeId, CriarUnidadeRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<UnidadeResponse>>> ListByPropriedadeAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<UnidadeResponse>> UpdateAsync(Guid proprietarioId, Guid unidadeId, AtualizarUnidadeRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid unidadeId, CancellationToken cancellationToken);
}
