using AppMorador.Application.Common;

namespace AppMorador.Application.Vagas;

public interface IVagaServico
{
    Task<Result<VagaResponse>> CreateAsync(Guid proprietarioId, Guid propriedadeId, CriarVagaRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VagaResponse>>> ListByPropriedadeAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<VagaResponse>> UpdateAsync(Guid proprietarioId, Guid vagaId, AtualizarVagaRequest request, CancellationToken cancellationToken);

    Task<Result<VagaResponse>> AtualizarStatusAsync(Guid proprietarioId, Guid vagaId, AtualizarStatusVagaRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid vagaId, CancellationToken cancellationToken);
}
