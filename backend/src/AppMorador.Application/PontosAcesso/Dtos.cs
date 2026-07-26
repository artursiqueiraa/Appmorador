using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.PontosAcesso;

public sealed class CriarPontoAcessoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Nulo = Geral (compatibilidade com o comportamento anterior à Sprint 9).</summary>
    public TipoPontoAcesso? Tipo { get; set; }
}

public sealed class AtualizarPontoAcessoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public TipoPontoAcesso? Tipo { get; set; }
}

public sealed class PontoAcessoResponse
{
    public required Guid Id { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string Nome { get; init; }

    public required TipoPontoAcesso Tipo { get; init; }
}
