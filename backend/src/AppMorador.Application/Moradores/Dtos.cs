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

    public string? Observacoes { get; init; }
}
