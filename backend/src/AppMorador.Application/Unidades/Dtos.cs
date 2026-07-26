using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Unidades;

public sealed class CriarUnidadeRequest
{
    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoUnidade Tipo { get; set; }

    [Required(ErrorMessage = "Identificação é obrigatória.")]
    [MaxLength(100)]
    public string Identificacao { get; set; } = string.Empty;
}

public sealed class AtualizarUnidadeRequest
{
    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoUnidade Tipo { get; set; }

    [Required(ErrorMessage = "Identificação é obrigatória.")]
    [MaxLength(100)]
    public string Identificacao { get; set; } = string.Empty;
}

public sealed class UnidadeResponse
{
    public required Guid Id { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required TipoUnidade Tipo { get; init; }

    public required string Identificacao { get; init; }
}
