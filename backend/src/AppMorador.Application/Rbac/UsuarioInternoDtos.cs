using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Rbac;

/// <summary>
/// Akteck Seg (ADR 0035, DIVIDA_TECNICA 54) — mesmas regras de entrada de
/// <c>CadastrarUsuarioRequest</c>: antes a API aceitava qualquer senha (até vazia)
/// para contas internas com acesso amplo ao Painel.
/// </summary>
public sealed class CriarUsuarioInternoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public required string Nome { get; init; }

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(256)]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "A senha deve ter ao menos 8 caracteres, com letra maiúscula, minúscula e número.")]
    public required string Senha { get; init; }

    [EnumDataType(typeof(RoleSistema), ErrorMessage = "Papel inválido.")]
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
