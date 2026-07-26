using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Vagas;

public sealed class CriarVagaRequest
{
    [Required(ErrorMessage = "Número é obrigatório.")]
    [MaxLength(20)]
    public string Numero { get; set; } = string.Empty;

    public string? Bloco { get; set; }

    public string? Andar { get; set; }

    public bool Coberta { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoVaga Tipo { get; set; }

    public string? Observacoes { get; set; }
}

public sealed class AtualizarVagaRequest
{
    [Required(ErrorMessage = "Número é obrigatório.")]
    [MaxLength(20)]
    public string Numero { get; set; } = string.Empty;

    public string? Bloco { get; set; }

    public string? Andar { get; set; }

    public bool Coberta { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoVaga Tipo { get; set; }

    public string? Observacoes { get; set; }
}

/// <summary>Só aceita Bloqueada, Reservada ou Livre (limpa o override manual) — Ocupada nunca é definida manualmente, é sempre computada a partir do vínculo ativo.</summary>
public sealed class AtualizarStatusVagaRequest
{
    [Required(ErrorMessage = "Status é obrigatório.")]
    public required StatusVaga Status { get; set; }
}

public sealed class VagaResponse
{
    public required Guid Id { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string Numero { get; init; }

    public string? Bloco { get; init; }

    public string? Andar { get; init; }

    public required bool Coberta { get; init; }

    public required TipoVaga Tipo { get; init; }

    public string? Observacoes { get; init; }

    /// <summary>Status efetivo (computado a partir do vínculo ativo quando não há override manual) — nunca o valor cru do banco.</summary>
    public required StatusVaga Status { get; init; }
}
