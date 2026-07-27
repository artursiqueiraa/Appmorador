using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class UsuarioPropriedadeRepositorio : IUsuarioPropriedadeRepositorio
{
    private readonly AppDbContext _db;

    public UsuarioPropriedadeRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<UsuarioPropriedade?> GetAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.UsuariosPropriedade.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId && v.PropriedadeId == propriedadeId, cancellationToken);

    public Task<UsuarioPropriedade?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.UsuariosPropriedade
            .Include(v => v.Usuario)
            .Include(v => v.Propriedade)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UsuarioPropriedade>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.UsuariosPropriedade
            .Include(v => v.Usuario)
            .Where(v => v.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(UsuarioPropriedade vinculo, CancellationToken cancellationToken) =>
        await _db.UsuariosPropriedade.AddAsync(vinculo, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
