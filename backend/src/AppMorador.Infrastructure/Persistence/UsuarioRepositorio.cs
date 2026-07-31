using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly AppDbContext _db;

    public UsuarioRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Usuario>> ListInternosAsync(CancellationToken cancellationToken) =>
        await _db.Usuarios
            .Where(u => u.RoleGlobal != null)
            .OrderBy(u => u.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> ExisteAlgumMasterAsync(CancellationToken cancellationToken) =>
        _db.Usuarios.AnyAsync(u => u.RoleGlobal == RoleSistema.Master, cancellationToken);

    public async Task<(IReadOnlyList<Usuario> Itens, int Total)> ListProprietariosAsync(
        int pagina, int tamanhoPagina, string? busca, CancellationToken cancellationToken)
    {
        var query = _db.Usuarios.Where(u => u.RoleGlobal == null);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(u => EF.Functions.Like(u.Nome, $"%{termo}%") || EF.Functions.Like(u.Email, $"%{termo}%"));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var itens = await query
            .OrderBy(u => u.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    public Task<int> ContarClientesAsync(CancellationToken cancellationToken) =>
        _db.Usuarios.CountAsync(u => u.RoleGlobal == null, cancellationToken);

    public async Task<IReadOnlyDictionary<string, int>> ContarClientesPorMesAsync(int meses, CancellationToken cancellationToken)
    {
        var desde = DateTime.UtcNow.AddMonths(-(meses - 1));
        desde = new DateTime(desde.Year, desde.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var clientes = await _db.Usuarios
            .Where(u => u.RoleGlobal == null && u.CreatedAtUtc >= desde)
            .Select(u => u.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return clientes
            .GroupBy(d => $"{d.Year:D4}-{d.Month:D2}")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken) =>
        await _db.Usuarios.AddAsync(usuario, cancellationToken).ConfigureAwait(false);

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken) =>
        await _db.RefreshTokens.AddAsync(refreshToken, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
