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

    public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken) =>
        await _db.Usuarios.AddAsync(usuario, cancellationToken).ConfigureAwait(false);

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken) =>
        await _db.RefreshTokens.AddAsync(refreshToken, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
