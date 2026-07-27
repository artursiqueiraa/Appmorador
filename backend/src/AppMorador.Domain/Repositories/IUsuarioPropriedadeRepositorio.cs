using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>
/// Porta DDD para o vínculo Usuario↔Propriedade (ADR 0021). Sprint 21 — único
/// consumidor real é a criação automática do vínculo Administrador (dono) quando uma
/// Propriedade é criada; nenhum código existente foi migrado para depender disto em
/// vez de <see cref="Propriedade.ProprietarioId"/>.
/// </summary>
public interface IUsuarioPropriedadeRepositorio
{
    Task<UsuarioPropriedade?> GetAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<UsuarioPropriedade?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UsuarioPropriedade>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(UsuarioPropriedade vinculo, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
