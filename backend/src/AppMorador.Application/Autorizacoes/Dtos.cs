using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Autorizacoes;

public sealed class CriarAutorizacaoRequest
{
    [Required(ErrorMessage = "Unidade é obrigatória.")]
    public required Guid UnidadeId { get; set; }

    [Required(ErrorMessage = "Morador responsável é obrigatório.")]
    public required Guid MoradorResponsavelId { get; set; }

    [Required(ErrorMessage = "Tipo de visita é obrigatório.")]
    public required TipoVisita Tipo { get; set; }

    [Required(ErrorMessage = "Data inicial é obrigatória.")]
    public required DateTime DataInicial { get; set; }

    [Required(ErrorMessage = "Data final é obrigatória.")]
    public required DateTime DataFinal { get; set; }

    public TimeOnly? HorarioInicial { get; set; }

    public TimeOnly? HorarioFinal { get; set; }
}

/// <summary>Unidade/Morador/Visitante não são editáveis — trocar quem/onde significa criar uma nova autorização (mesmo espírito de Credencial.Tipo imutável, ADR 0010).</summary>
public sealed class AtualizarAutorizacaoRequest
{
    [Required(ErrorMessage = "Tipo de visita é obrigatório.")]
    public required TipoVisita Tipo { get; set; }

    [Required(ErrorMessage = "Data inicial é obrigatória.")]
    public required DateTime DataInicial { get; set; }

    [Required(ErrorMessage = "Data final é obrigatória.")]
    public required DateTime DataFinal { get; set; }

    public TimeOnly? HorarioInicial { get; set; }

    public TimeOnly? HorarioFinal { get; set; }
}

/// <summary>Só aceita Cancelada ou Utilizada — Pendente/Ativa/Expirada são computados, nunca definidos manualmente.</summary>
public sealed class AtualizarStatusAutorizacaoRequest
{
    [Required(ErrorMessage = "Status é obrigatório.")]
    public required StatusAutorizacao Status { get; set; }
}

public sealed class AutorizacaoResponse
{
    public required Guid Id { get; init; }

    public required Guid MoradorResponsavelId { get; init; }

    public required string MoradorResponsavelNome { get; init; }

    public required Guid UnidadeId { get; init; }

    public required string UnidadeIdentificacao { get; init; }

    public required Guid VisitanteId { get; init; }

    public required string VisitanteNome { get; init; }

    public required TipoVisita Tipo { get; init; }

    public required DateTime DataInicial { get; init; }

    public required DateTime DataFinal { get; init; }

    public TimeOnly? HorarioInicial { get; init; }

    public TimeOnly? HorarioFinal { get; init; }

    /// <summary>Status efetivo (computado a partir das datas quando não há override manual) — nunca o valor cru do banco.</summary>
    public required StatusAutorizacao Status { get; init; }
}
