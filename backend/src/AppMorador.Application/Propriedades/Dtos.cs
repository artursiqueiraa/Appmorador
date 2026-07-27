using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Propriedades;

public sealed class CriarPropriedadeRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    public required TipoPropriedade Tipo { get; set; }

    [MaxLength(300)]
    public string? Endereco { get; set; }
}

public sealed class AtualizarPropriedadeRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    public required TipoPropriedade Tipo { get; set; }

    [MaxLength(300)]
    public string? Endereco { get; set; }
}

public sealed class PropriedadeResponse
{
    public required Guid Id { get; init; }

    public required string Nome { get; init; }

    public required TipoPropriedade Tipo { get; init; }

    public string? Endereco { get; init; }

    /// <summary>
    /// Sprint 21 (ADR 0021/0025/0026) — o app mobile consulta isto no login/troca de
    /// propriedade (GET /api/properties) para decidir o que mostrar/esconder, sem
    /// nunca confiar só no papel — ver usePermissao no app. Só populado de verdade em
    /// ListByOwnerAsync (Create/Update/Delete devolvem a Propriedade recém-alterada,
    /// não precisam desta consulta extra).
    /// </summary>
    public PerfilPropriedade Perfil { get; init; } = PerfilPropriedade.Administrador;

    public IReadOnlyList<PermissaoFuncionalidade> Permissoes { get; init; } = [];

    public IReadOnlyList<FeatureFlag> Features { get; init; } = [];
}

/// <summary>Sprint 21 (ADR 0026).</summary>
public sealed class DefinirFeatureFlagRequest
{
    public required bool Ativo { get; init; }
}
