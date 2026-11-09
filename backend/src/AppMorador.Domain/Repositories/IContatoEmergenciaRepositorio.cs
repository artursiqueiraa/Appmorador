using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Sprint 24 (ADR 0040) — porta dos contatos de emergência; o filtro de soft delete é global no DbContext.</summary>
public interface IContatoEmergenciaRepositorio
{
    Task<ContatoEmergencia?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Ordenados por <see cref="ContatoEmergencia.Ordem"/>.</summary>
    Task<IReadOnlyList<ContatoEmergencia>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(ContatoEmergencia contato, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
