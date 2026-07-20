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
}
