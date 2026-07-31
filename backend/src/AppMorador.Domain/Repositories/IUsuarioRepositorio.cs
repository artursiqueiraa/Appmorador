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

    /// <summary>
    /// Sprint 22A (ADR 0029) — clientes da plataforma (RoleGlobal nulo — nunca inclui contas
    /// internas), paginado, com busca opcional por nome/e-mail. Master/Suporte-only (ver
    /// Policies.RequerSuporte no Controller).
    /// </summary>
    Task<(IReadOnlyList<Usuario> Itens, int Total)> ListProprietariosAsync(
        int pagina, int tamanhoPagina, string? busca, CancellationToken cancellationToken);

    /// <summary>Sprint 22A (ADR 0029) — total de clientes (RoleGlobal nulo) para o Dashboard Operacional.</summary>
    Task<int> ContarClientesAsync(CancellationToken cancellationToken);

    /// <summary>Sprint 22A (ADR 0029) — novos clientes por mês, últimos N meses (chave "AAAA-MM").</summary>
    Task<IReadOnlyDictionary<string, int>> ContarClientesPorMesAsync(int meses, CancellationToken cancellationToken);

    Task AddAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>Busca um refresh token ativo ou nao pelo hash — quem chama decide o que fazer com o estado.</summary>
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
