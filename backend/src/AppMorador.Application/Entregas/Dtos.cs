using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Entregas;

public sealed class CriarEntregaRequest
{
    [Required(ErrorMessage = "Unidade é obrigatória.")]
    public required Guid UnidadeId { get; set; }

    [Required(ErrorMessage = "Morador destinatário é obrigatório.")]
    public required Guid MoradorDestinatarioId { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoEntrega Tipo { get; set; }

    public string? Descricao { get; set; }

    public string? Observacoes { get; set; }
}

/// <summary>Unidade/Morador destinatário não são editáveis — trocar quem/onde significa registrar uma nova entrega (mesmo espírito de Autorizacao, ADR 0011).</summary>
public sealed class AtualizarEntregaRequest
{
    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoEntrega Tipo { get; set; }

    public string? Descricao { get; set; }

    public string? Observacoes { get; set; }
}

/// <summary>Só aceita a transição válida a partir do status atual — ver ADR 0013 (máquina de estados 100% manual, sem status computado).</summary>
public sealed class AtualizarStatusEntregaRequest
{
    [Required(ErrorMessage = "Status é obrigatório.")]
    public required StatusEntrega Status { get; set; }

    /// <summary>Só usado quando Status = DisponivelParaRetirada — quem recebeu fisicamente a entrega.</summary>
    public string? RecebidoPor { get; set; }
}

public sealed class EntregaResponse
{
    public required Guid Id { get; init; }

    public required Guid MoradorDestinatarioId { get; init; }

    public required string MoradorDestinatarioNome { get; init; }

    public required Guid UnidadeId { get; init; }

    public required string UnidadeIdentificacao { get; init; }

    public required TipoEntrega Tipo { get; init; }

    public string? Descricao { get; init; }

    public string? RecebidoPor { get; init; }

    public DateTime? DataRecebimentoUtc { get; init; }

    public DateTime? DataRetiradaUtc { get; init; }

    public string? Observacoes { get; init; }

    public required StatusEntrega Status { get; init; }
}
