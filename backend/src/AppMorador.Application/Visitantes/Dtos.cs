using System.ComponentModel.DataAnnotations;

namespace AppMorador.Application.Visitantes;

public sealed class CriarVisitanteRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    public string? Documento { get; set; }

    public string? Telefone { get; set; }

    public string? Observacoes { get; set; }
}

public sealed class AtualizarVisitanteRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    public string? Documento { get; set; }

    public string? Telefone { get; set; }

    public string? Observacoes { get; set; }
}

public sealed class VisitanteResponse
{
    public required Guid Id { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string Nome { get; init; }

    public string? Documento { get; init; }

    public string? Telefone { get; init; }

    public string? FotoPath { get; init; }

    public string? Observacoes { get; init; }
}
