namespace AppMorador.Application.Notificacoes;

/// <summary>
/// Sprint 19 (ADR 0023) — os únicos tipos de notificação que o backend sabe
/// construir de verdade, cada um com um ponto de disparo real na Application
/// (nunca inferido de um sinal genérico como <c>EquipamentoStatusAlterado</c> — ver
/// ADR 0023 "Por que não notificar a partir do Snapshot Operacional"). Não existe
/// <c>EquipamentoOnline</c> aqui: a própria missão desta Sprint decidiu não
/// notificar esse caso.
/// </summary>
public enum EventoNotificacaoTipo
{
    AlarmeDisparado,
    SistemaArmado,
    SistemaDesarmado,
    ComandoAcionado,
    VisitanteAutorizado,
    EntregaRecebida,
    EquipamentoOffline,
}
