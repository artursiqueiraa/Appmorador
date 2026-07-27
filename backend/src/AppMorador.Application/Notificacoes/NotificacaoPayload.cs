namespace AppMorador.Application.Notificacoes;

public enum PrioridadeNotificacao
{
    Baixa,
    Normal,
    Alta,
}

/// <summary>
/// Sprint 19 (ADR 0023) — forma que o <see cref="INotificationProvider"/> recebe
/// para montar a mensagem real (Firebase ou qualquer outro). Nunca contém nada
/// específico de um provedor (sem "topic" do FCM, sem "collapse key" — esses
/// detalhes são resolvidos dentro do provider concreto).
/// </summary>
public sealed class NotificacaoPayload
{
    public required string Titulo { get; init; }

    public required string Corpo { get; init; }

    public required EventoNotificacaoTipo EventoTipo { get; init; }

    public required Guid PropriedadeId { get; init; }

    public Guid? EquipamentoId { get; init; }

    /// <summary>Ação de deep link para o Mobile decidir para onde navegar ao tocar — ver ADR 0023.</summary>
    public required string Acao { get; init; }

    public PrioridadeNotificacao Prioridade { get; init; } = PrioridadeNotificacao.Normal;
}

/// <summary>Contexto necessário para o <see cref="INotificationDispatcher"/> montar a mensagem certa por tipo de evento.</summary>
public sealed class ContextoNotificacao
{
    public required Guid PropriedadeId { get; init; }

    public required string NomePropriedade { get; init; }

    public Guid? EquipamentoId { get; init; }

    public string? NomeEquipamento { get; init; }

    /// <summary>Nome do visitante autorizado, etc. — só relevante para alguns tipos de evento.</summary>
    public string? NomeContextual { get; init; }
}
