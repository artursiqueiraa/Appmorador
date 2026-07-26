using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.PermissoesAcesso;

public sealed class CriarPermissaoAcessoRequest
{
    [Required(ErrorMessage = "Ponto de acesso é obrigatório.")]
    public required Guid PontoAcessoId { get; set; }

    /// <summary>Nulo = sem restrição de dia (todos os dias).</summary>
    public DiaSemana? DiasPermitidos { get; set; }

    public TimeOnly? HorarioInicial { get; set; }

    public TimeOnly? HorarioFinal { get; set; }

    public DateTime? DataInicial { get; set; }

    public DateTime? DataFinal { get; set; }
}

public sealed class AtualizarPermissaoAcessoRequest
{
    public DiaSemana? DiasPermitidos { get; set; }

    public TimeOnly? HorarioInicial { get; set; }

    public TimeOnly? HorarioFinal { get; set; }

    public DateTime? DataInicial { get; set; }

    public DateTime? DataFinal { get; set; }
}

public sealed class PermissaoAcessoResponse
{
    public required Guid Id { get; init; }

    public required Guid CredencialId { get; init; }

    public required Guid PontoAcessoId { get; init; }

    public required string PontoAcessoNome { get; init; }

    public required DiaSemana DiasPermitidos { get; init; }

    public TimeOnly? HorarioInicial { get; init; }

    public TimeOnly? HorarioFinal { get; init; }

    public DateTime? DataInicial { get; init; }

    public DateTime? DataFinal { get; init; }
}
