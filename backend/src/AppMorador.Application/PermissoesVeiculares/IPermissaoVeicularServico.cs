using AppMorador.Application.Common;

namespace AppMorador.Application.PermissoesVeiculares;

public interface IPermissaoVeicularServico
{
    Task<Result<PermissaoVeicularResponse>> CreateAsync(Guid proprietarioId, Guid veiculoId, CriarPermissaoVeicularRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PermissaoVeicularResponse>>> ListByVeiculoAsync(Guid proprietarioId, Guid veiculoId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid permissaoId, CancellationToken cancellationToken);
}
