using AppMorador.Application.Common;

namespace AppMorador.Application.Moradores;

public interface IMoradorServico
{
    Task<Result<MoradorResponse>> CreateAsync(Guid proprietarioId, Guid unidadeId, CriarMoradorRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<MoradorResponse>>> ListByUnidadeAsync(Guid proprietarioId, Guid unidadeId, CancellationToken cancellationToken);

    Task<Result<MoradorResponse>> UpdateAsync(Guid proprietarioId, Guid moradorId, AtualizarMoradorRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken);
}
