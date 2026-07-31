namespace AppMorador.Application.Painel;

/// <summary>Sprint 22A (ADR 0029) — agregado cross-propriedade para o Dashboard Operacional. Master/Suporte-only.</summary>
public interface IDashboardOperacionalServico
{
    Task<DashboardOperacionalResponse> ObterAsync(CancellationToken cancellationToken);
}
