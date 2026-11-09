using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Sprint 25 (ADR 0041) — convites de acesso a propriedades.</summary>
public interface IConvitePropriedadeRepositorio
{
    Task<ConvitePropriedade?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Com a propriedade carregada (nome para a tela de aceite).</summary>
    Task<ConvitePropriedade?> GetByCodigoHashAsync(string codigoHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConvitePropriedade>> ListPendentesAsync(Guid propriedadeId, DateTime agoraUtc, CancellationToken cancellationToken);

    Task AddAsync(ConvitePropriedade convite, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
