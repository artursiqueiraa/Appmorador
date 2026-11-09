using AppMorador.Domain.Entities;

namespace AppMorador.Application.Alertas;

/// <summary>Sprint 24 (ADR 0040) — dados para abrir um alerta crítico (central, pânico do app ou vigia de conexão).</summary>
public sealed class NovoAlertaCritico
{
    public required Guid PropriedadeId { get; init; }

    public required TipoAlertaCritico Tipo { get; init; }

    public required string Descricao { get; init; }

    public Guid? OcorrenciaId { get; init; }

    public Guid? EquipamentoId { get; init; }

    public Guid? AcionadoPorUsuarioId { get; init; }
}

public sealed class AlertaCriticoResponse
{
    public required Guid Id { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required TipoAlertaCritico Tipo { get; init; }

    /// <summary>"Alarme disparado", "Pânico acionado"… — pronto para a tela.</summary>
    public required string Titulo { get; init; }

    public required string Descricao { get; init; }

    public required DateTime CriadoEmUtc { get; init; }

    public required bool Confirmado { get; init; }

    public DateTime? ConfirmadoEmUtc { get; init; }

    public string? ConfirmadoPorNome { get; init; }

    public required NivelEscalonamentoAlerta NivelEscalonamento { get; init; }

    public DateTime? ProximoEscalonamentoEmUtc { get; init; }

    public DateTime? EscalonadoMembrosEmUtc { get; init; }

    public DateTime? EscalonadoContatosEmUtc { get; init; }

    public bool AcionadoPeloApp { get; init; }

    public static string TituloDe(TipoAlertaCritico tipo) => tipo switch
    {
        TipoAlertaCritico.Disparo => "Alarme disparado",
        TipoAlertaCritico.Panico => "Pânico acionado",
        TipoAlertaCritico.Violacao => "Tentativa de violação",
        TipoAlertaCritico.SemComunicacao => "Central sem comunicação",
        _ => "Alerta",
    };

    public static AlertaCriticoResponse FromEntity(AlertaCritico alerta) => new()
    {
        Id = alerta.Id,
        PropriedadeId = alerta.PropriedadeId,
        Tipo = alerta.Tipo,
        Titulo = TituloDe(alerta.Tipo),
        Descricao = alerta.Descricao,
        CriadoEmUtc = alerta.CriadoEmUtc,
        Confirmado = alerta.Confirmado,
        ConfirmadoEmUtc = alerta.ConfirmadoEmUtc,
        ConfirmadoPorNome = alerta.ConfirmadoPorUsuario?.Nome,
        NivelEscalonamento = alerta.NivelEscalonamento,
        ProximoEscalonamentoEmUtc = alerta.ProximoEscalonamentoEmUtc,
        EscalonadoMembrosEmUtc = alerta.EscalonadoMembrosEmUtc,
        EscalonadoContatosEmUtc = alerta.EscalonadoContatosEmUtc,
        AcionadoPeloApp = alerta.AcionadoPorUsuarioId is not null,
    };
}

public sealed class ConfiguracaoAlertasResponse
{
    public required int MinutosEscalonamento { get; init; }
}

public sealed class AtualizarConfiguracaoAlertasRequest
{
    public int MinutosEscalonamento { get; init; }
}
