using AppMorador.Application.Common;

namespace AppMorador.Application.Autorizacoes;

public interface IAutorizacaoServico
{
    Task<Result<AutorizacaoResponse>> CreateAsync(Guid proprietarioId, Guid visitanteId, CriarAutorizacaoRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AutorizacaoResponse>>> ListByVisitanteAsync(Guid proprietarioId, Guid visitanteId, CancellationToken cancellationToken);

    Task<Result<AutorizacaoResponse>> UpdateAsync(Guid proprietarioId, Guid autorizacaoId, AtualizarAutorizacaoRequest request, CancellationToken cancellationToken);

    Task<Result<AutorizacaoResponse>> AtualizarStatusAsync(Guid proprietarioId, Guid autorizacaoId, AtualizarStatusAutorizacaoRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid autorizacaoId, CancellationToken cancellationToken);
}
