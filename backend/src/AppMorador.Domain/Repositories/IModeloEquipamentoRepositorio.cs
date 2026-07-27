using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o catálogo de ModeloEquipamento + suas capacidades (ADR 0027).</summary>
public interface IModeloEquipamentoRepositorio
{
    Task<ModeloEquipamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Usado para o get-or-create transparente a partir do texto livre já enviado pelo cliente (Fabricante, Nome) — nunca duplica um modelo já catalogado.</summary>
    Task<ModeloEquipamento?> GetByFabricanteENomeAsync(FabricanteEquipamento fabricante, string nome, CancellationToken cancellationToken);

    Task<IReadOnlyList<ModeloEquipamento>> ListByFabricanteAsync(FabricanteEquipamento? fabricante, CancellationToken cancellationToken);

    Task<IReadOnlyList<EquipamentoCapacidade>> ListCapacidadesAsync(Guid modeloEquipamentoId, CancellationToken cancellationToken);

    /// <summary>Substitui o conjunto inteiro de capacidades do modelo — mesma semântica de "replace-all" já usada para inibição de zonas JFL.</summary>
    Task SubstituirCapacidadesAsync(Guid modeloEquipamentoId, IReadOnlyCollection<EquipamentoCapacidade> capacidades, CancellationToken cancellationToken);

    Task AddAsync(ModeloEquipamento modelo, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
