using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o snapshot de status JFL (1:1 com Equipamento) — implementação (EF Core) vive em Infrastructure.</summary>
public interface IStatusCentralJflRepositorio
{
    Task<StatusCentralJfl?> GetByEquipamentoIdAsync(Guid equipamentoId, CancellationToken cancellationToken);

    /// <summary>Usado pelo Dashboard — soma os rollups de todas as centrais JFL de uma propriedade.</summary>
    Task<IReadOnlyList<StatusCentralJfl>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>Insere ou substitui o snapshot existente do Equipamento (upsert, sempre 1:1).</summary>
    Task UpsertAsync(StatusCentralJfl status, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
