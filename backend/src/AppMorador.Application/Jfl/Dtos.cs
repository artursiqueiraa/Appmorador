using System.ComponentModel.DataAnnotations;

namespace AppMorador.Application.Jfl;

public sealed class ParticaoRequest
{
    [Required(ErrorMessage = "Partição é obrigatória.")]
    public required int Particao { get; set; }
}

public sealed class PgmRequest
{
    [Required(ErrorMessage = "Número da PGM é obrigatório.")]
    public required int PgmNumero { get; set; }
}

public sealed class ZonaRequest
{
    [Required(ErrorMessage = "Número da zona é obrigatório.")]
    public required int ZonaNumero { get; set; }
}

public sealed class ResultadoTesteConexaoJfl
{
    public required bool Sucesso { get; init; }

    public string? MensagemErro { get; init; }
}

public sealed class ParticaoStatusInfo
{
    public required int Numero { get; init; }

    public required bool Desabilitada { get; init; }

    public required bool Armada { get; init; }

    public required bool ArmadaStay { get; init; }

    public required bool EmDisparo { get; init; }
}

public sealed class ZonaStatusInfo
{
    public required int Numero { get; init; }

    /// <summary>Nome amigável do estado (Aberta/Fechada/Inibida/Disparo/...) — nunca o nibble cru.</summary>
    public required string Estado { get; init; }

    public required bool PermiteInibir { get; init; }
}

public sealed class PgmStatusInfo
{
    public required int Numero { get; init; }

    public required bool Acionada { get; init; }

    public required bool Permitida { get; init; }
}

public sealed class StatusCentralJflInfo
{
    public DateTime? DataHoraCentral { get; init; }

    public required string BateriaTipo { get; init; }

    public int? BateriaPercentual { get; init; }

    public double? BateriaTensaoAproximada { get; init; }

    public required bool EletrificadorArmado { get; init; }

    public required IReadOnlyList<ParticaoStatusInfo> Particoes { get; init; }

    public required IReadOnlyList<ZonaStatusInfo> Zonas { get; init; }

    public required IReadOnlyList<PgmStatusInfo> Pgms { get; init; }

    /// <summary>Só os problemas ativos (bit ligado), já com nome amigável — nunca a lista completa de 34 flags.</summary>
    public required IReadOnlyList<string> ProblemasAtivos { get; init; }
}

public sealed class ResultadoComandoJfl
{
    public required bool Sucesso { get; init; }

    public string? MensagemErro { get; init; }

    public StatusCentralJflInfo? StatusResultante { get; init; }
}
