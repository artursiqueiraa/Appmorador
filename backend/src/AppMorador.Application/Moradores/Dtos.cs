using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Moradores;

public sealed class CriarMoradorRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Documento { get; set; }

    /// <summary>Akteck Seg (ADR 0036) — titular ou dependente. Padrão: Titular.</summary>
    [EnumDataType(typeof(TipoMorador), ErrorMessage = "Tipo inválido.")]
    public TipoMorador Tipo { get; set; } = TipoMorador.Titular;

    /// <summary>Só para dependentes; ignorado (gravado nulo) para titulares.</summary>
    [EnumDataType(typeof(Parentesco), ErrorMessage = "Parentesco inválido.")]
    public Parentesco? Parentesco { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}

public sealed class AtualizarMoradorRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Documento { get; set; }

    [Required(ErrorMessage = "Status é obrigatório.")]
    public required StatusMorador Status { get; set; }

    /// <summary>Akteck Seg (ADR 0036) — titular ou dependente. Padrão: Titular.</summary>
    [EnumDataType(typeof(TipoMorador), ErrorMessage = "Tipo inválido.")]
    public TipoMorador Tipo { get; set; } = TipoMorador.Titular;

    /// <summary>Só para dependentes; ignorado (gravado nulo) para titulares.</summary>
    [EnumDataType(typeof(Parentesco), ErrorMessage = "Parentesco inválido.")]
    public Parentesco? Parentesco { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}

public sealed class MoradorResponse
{
    public required Guid Id { get; init; }

    public required Guid UnidadeId { get; init; }

    public required string Nome { get; init; }

    public string? FotoPath { get; init; }

    public string? Telefone { get; init; }

    public string? Email { get; init; }

    public string? Documento { get; init; }

    public required StatusMorador Status { get; init; }

    public required TipoMorador Tipo { get; init; }

    public Parentesco? Parentesco { get; init; }

    /// <summary>Akteck Seg (ADR 0036) — há foto de rosto salva no servidor (servida em <c>GET api/moradores/{id}/foto</c>).</summary>
    public bool TemFoto { get; init; }

    public string? Observacoes { get; init; }
}
