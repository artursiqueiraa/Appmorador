using AppMorador.Application.Common;

namespace AppMorador.Application.Entregas;

public interface IEntregaServico
{
    Task<Result<EntregaResponse>> CreateAsync(Guid proprietarioId, Guid propriedadeId, CriarEntregaRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<EntregaResponse>>> ListByPropriedadeAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<EntregaResponse>> GetByIdAsync(Guid proprietarioId, Guid entregaId, CancellationToken cancellationToken);

    Task<Result<EntregaResponse>> UpdateAsync(Guid proprietarioId, Guid entregaId, AtualizarEntregaRequest request, CancellationToken cancellationToken);

    Task<Result<EntregaResponse>> AtualizarStatusAsync(Guid proprietarioId, Guid entregaId, AtualizarStatusEntregaRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid entregaId, CancellationToken cancellationToken);
}
