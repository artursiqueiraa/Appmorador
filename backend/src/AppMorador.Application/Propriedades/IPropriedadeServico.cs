using AppMorador.Application.Common;

namespace AppMorador.Application.Propriedades;

public interface IPropriedadeServico
{
    Task<PropriedadeResponse> CreateAsync(Guid proprietarioId, CriarPropriedadeRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<PropriedadeResponse>> ListByOwnerAsync(Guid proprietarioId, CancellationToken cancellationToken);

    Task<Result<PropriedadeResponse>> UpdateAsync(
        Guid proprietarioId, Guid propriedadeId, AtualizarPropriedadeRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);
}
