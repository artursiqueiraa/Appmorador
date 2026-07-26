using AppMorador.Application.Common;

namespace AppMorador.Application.PontosAcesso;

public interface IPontoAcessoServico
{
    Task<Result<PontoAcessoResponse>> CreateAsync(Guid proprietarioId, Guid propriedadeId, CriarPontoAcessoRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PontoAcessoResponse>>> ListByPropriedadeAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<PontoAcessoResponse>> UpdateAsync(Guid proprietarioId, Guid pontoAcessoId, AtualizarPontoAcessoRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid pontoAcessoId, CancellationToken cancellationToken);
}
