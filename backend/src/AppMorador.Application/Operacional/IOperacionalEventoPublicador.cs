using AppMorador.Application.Eventos;

namespace AppMorador.Application.Operacional;

/// <summary>Motivo de uma publicação em tempo real (Sprint 14, ADR 0017) — só para observabilidade/log, nunca para lógica de negócio no lado do cliente.</summary>
public enum MotivoAtualizacaoOperacional
{
    EquipamentoStatusAlterado,
    AlarmeDisparado,
    SnapshotAtualizadoManualmente,
}

/// <summary>
/// Porta de publicação em tempo real (Sprint 14, ADR 0017). Transporte (SignalR ou
/// qualquer outro) é responsabilidade exclusiva de Infrastructure/Api — o domínio
/// nunca conhece SignalR. Se nenhuma implementação real for registrada, o sistema
/// continua funcionando integralmente (só sem notificação automática).
/// </summary>
public interface IOperacionalEventoPublicador
{
    /// <summary>Publica um Snapshot Operacional já gerado e persistido — nunca calcula nada, só transporta o DTO pronto.</summary>
    Task PublicarSnapshotAsync(Guid propriedadeId, SnapshotOperacionalResponse snapshot, MotivoAtualizacaoOperacional motivo, CancellationToken cancellationToken);

    /// <summary>Publica um evento novo da Central de Eventos já persistido — nunca cria ou classifica o evento.</summary>
    Task PublicarNovoEventoAsync(Guid propriedadeId, EventoResponse evento, CancellationToken cancellationToken);
}
