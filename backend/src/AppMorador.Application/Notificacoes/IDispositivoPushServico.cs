using AppMorador.Application.Common;

namespace AppMorador.Application.Notificacoes;

/// <summary>Sprint 19 (ADR 0023) — ciclo de vida do token de push visto do lado do Mobile (registrar/atualizar/desativar). Nunca deleta fisicamente — ver <see cref="Domain.Entities.DispositivoPush"/>.</summary>
public interface IDispositivoPushServico
{
    /// <summary>Se o token já existir (reinstalação, mesmo dispositivo), atualiza em vez de duplicar — nunca dois registros ativos para o mesmo token físico.</summary>
    Task<DispositivoPushResponse> RegistrarAsync(Guid usuarioId, RegistrarDispositivoPushRequest request, CancellationToken cancellationToken);

    Task<Result<DispositivoPushResponse>> AtualizarTokenAsync(Guid usuarioId, Guid id, AtualizarDispositivoPushRequest request, CancellationToken cancellationToken);

    Task<Result<DispositivoPushResponse>> AtualizarPreferenciasAsync(Guid usuarioId, Guid id, AtualizarPreferenciasDispositivoPushRequest request, CancellationToken cancellationToken);

    /// <summary>Marca Ativo=false — chamado no logout. Idempotente (desativar um dispositivo já inativo não é erro).</summary>
    Task<Result> DesativarAsync(Guid usuarioId, Guid id, CancellationToken cancellationToken);
}
