using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

public interface IUsuarioPropriedadePermissaoRepositorio
{
    Task<IReadOnlyList<PermissaoFuncionalidade>> ListAsync(Guid usuarioPropriedadeId, CancellationToken cancellationToken);

    Task<bool> TemPermissaoAsync(Guid usuarioPropriedadeId, PermissaoFuncionalidade permissao, CancellationToken cancellationToken);

    /// <summary>Substitui o conjunto inteiro de permissões concedidas — mesma semântica "replace-all" já usada para capacidades de equipamento e inibição de zonas JFL.</summary>
    Task SubstituirAsync(Guid usuarioPropriedadeId, IReadOnlyCollection<PermissaoFuncionalidade> permissoes, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
