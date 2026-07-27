using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Usuario — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IUsuarioRepositorio
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Sprint 21 (ADR 0021) — contas internas da plataforma (RoleGlobal preenchido) — nunca inclui clientes.</summary>
    Task<IReadOnlyList<Usuario>> ListInternosAsync(CancellationToken cancellationToken);

    Task<bool> ExisteAlgumMasterAsync(CancellationToken cancellationToken);

    Task AddAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>Busca um refresh token ativo ou nao pelo hash — quem chama decide o que fazer com o estado.</summary>
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
