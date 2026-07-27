using AppMorador.Application.Common;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Rbac;

/// <summary>Sprint 21 (ADR 0025) — gestão das Permissões Funcionais de um vínculo Usuario↔Propriedade. Exclusivo de Técnico/Master (até o Painel Web/autoatendimento existir).</summary>
public interface IUsuarioPropriedadePermissaoServico
{
    Task<Result<IReadOnlyList<PermissaoFuncionalidade>>> ListarAsync(Guid propriedadeId, Guid usuarioAlvoId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PermissaoFuncionalidade>>> DefinirAsync(
        Guid propriedadeId, Guid usuarioAlvoId, IReadOnlyCollection<PermissaoFuncionalidade> permissoes, CancellationToken cancellationToken);
}
