using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Equipamento — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IEquipamentoRepositorio
{
    Task<Equipamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Equipamento>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusEquipamento? status, CancellationToken cancellationToken);

    /// <summary>Usado pelo Dashboard — equipamento com a sincronização mais recente entre todos os da propriedade.</summary>
    Task<DateTime?> GetUltimaSincronizacaoAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(Equipamento equipamento, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
