using AppMorador.Domain.Entities;

namespace AppMorador.Application.Rbac;

public sealed class CriarUsuarioInternoRequest
{
    public required string Nome { get; init; }

    public required string Email { get; init; }

    public required string Senha { get; init; }

    public required RoleSistema RoleGlobal { get; init; }
}

public sealed class UsuarioInternoResponse
{
    public Guid Id { get; init; }

    public required string Nome { get; init; }

    public required string Email { get; init; }

    public required RoleSistema RoleGlobal { get; init; }

    public bool Ativo { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public static UsuarioInternoResponse FromEntity(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nome = usuario.Nome,
        Email = usuario.Email,
        RoleGlobal = usuario.RoleGlobal!.Value,
        Ativo = usuario.Ativo,
        CreatedAtUtc = usuario.CreatedAtUtc,
    };
}

/// <summary>Sprint 21 (ADR 0025).</summary>
public sealed class DefinirPermissoesRequest
{
    public required IReadOnlyCollection<PermissaoFuncionalidade> Permissoes { get; init; }
}
