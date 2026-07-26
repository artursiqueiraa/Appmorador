using AppMorador.Application.Common;

namespace AppMorador.Application.PermissoesAcesso;

public interface IPermissaoAcessoServico
{
    Task<Result<PermissaoAcessoResponse>> CreateAsync(Guid proprietarioId, Guid credencialId, CriarPermissaoAcessoRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PermissaoAcessoResponse>>> ListByCredencialAsync(Guid proprietarioId, Guid credencialId, CancellationToken cancellationToken);

    Task<Result<PermissaoAcessoResponse>> UpdateAsync(Guid proprietarioId, Guid permissaoId, AtualizarPermissaoAcessoRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid permissaoId, CancellationToken cancellationToken);
}
