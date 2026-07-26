namespace AppMorador.Domain.Entities;

/// <summary>Transições 100% manuais (comandadas pelo usuário) — sem job/scheduler, ver ADR 0013.</summary>
public enum StatusEntrega
{
    AguardandoRecebimento,
    DisponivelParaRetirada,
    Retirada,
    Cancelada,
}
