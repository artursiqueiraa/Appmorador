using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o log de eventos importados de equipamentos — auditoria pura, nunca excluida.</summary>
public interface IEventoEquipamentoRepositorio
{
    Task<IReadOnlyList<EventoEquipamento>> ListByEquipamentoAsync(Guid equipamentoId, CancellationToken cancellationToken);

    /// <summary>Usado pelo Dashboard — evento mais recente entre todos os equipamentos da propriedade.</summary>
    Task<DateTime?> GetUltimoRecebidoAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyList<EventoEquipamento> eventos, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
