using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Rbac;

public sealed class UsuarioPropriedadePermissaoServico : IUsuarioPropriedadePermissaoServico
{
    private readonly IUsuarioPropriedadeRepositorio _vinculos;
    private readonly IUsuarioPropriedadePermissaoRepositorio _permissoes;

    public UsuarioPropriedadePermissaoServico(IUsuarioPropriedadeRepositorio vinculos, IUsuarioPropriedadePermissaoRepositorio permissoes)
    {
        _vinculos = vinculos;
        _permissoes = permissoes;
    }

    public async Task<Result<IReadOnlyList<PermissaoFuncionalidade>>> ListarAsync(Guid propriedadeId, Guid usuarioAlvoId, CancellationToken cancellationToken)
    {
        var vinculo = await _vinculos.GetAsync(usuarioAlvoId, propriedadeId, cancellationToken).ConfigureAwait(false);
        if (vinculo is null)
        {
            return Result<IReadOnlyList<PermissaoFuncionalidade>>.Fail("Vínculo usuário↔propriedade não encontrado.");
        }

        return Result<IReadOnlyList<PermissaoFuncionalidade>>.Ok(await _permissoes.ListAsync(vinculo.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<IReadOnlyList<PermissaoFuncionalidade>>> DefinirAsync(
        Guid propriedadeId, Guid usuarioAlvoId, IReadOnlyCollection<PermissaoFuncionalidade> permissoes, CancellationToken cancellationToken)
    {
        var vinculo = await _vinculos.GetAsync(usuarioAlvoId, propriedadeId, cancellationToken).ConfigureAwait(false);
        if (vinculo is null)
        {
            return Result<IReadOnlyList<PermissaoFuncionalidade>>.Fail("Vínculo usuário↔propriedade não encontrado.");
        }

        await _permissoes.SubstituirAsync(vinculo.Id, permissoes, cancellationToken).ConfigureAwait(false);
        await _permissoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<PermissaoFuncionalidade>>.Ok(await _permissoes.ListAsync(vinculo.Id, cancellationToken).ConfigureAwait(false));
    }
}
