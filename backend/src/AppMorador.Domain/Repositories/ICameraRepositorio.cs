using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>
/// Porta DDD para o agregado Camera — a implementacao (EF Core) vive em Infrastructure.
/// Sprint 20 — primeiro consumidor "de aplicativo" de Camera (antes disso, só
/// <c>CameraResolver</c>, exclusivo do fluxo de captura disparado por alarme, consultava
/// esta tabela).
/// </summary>
public interface ICameraRepositorio
{
    /// <summary>Inclui Propriedade (ownership) e Gravador — quem chama decide o que usar.</summary>
    Task<Camera?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Camera>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
