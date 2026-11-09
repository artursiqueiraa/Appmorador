using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class ConvitePropriedadeRepositorio : IConvitePropriedadeRepositorio
{
    private readonly AppDbContext _db;

    public ConvitePropriedadeRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<ConvitePropriedade?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.ConvitesPropriedade.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<ConvitePropriedade?> GetByCodigoHashAsync(string codigoHash, CancellationToken cancellationToken) =>
        _db.ConvitesPropriedade.Include(c => c.Propriedade).FirstOrDefaultAsync(c => c.CodigoHash == codigoHash, cancellationToken);

    public async Task<IReadOnlyList<ConvitePropriedade>> ListPendentesAsync(Guid propriedadeId, DateTime agoraUtc, CancellationToken cancellationToken) =>
        await _db.ConvitesPropriedade
            .AsNoTracking()
            .Where(c => c.PropriedadeId == propriedadeId && c.AceitoEmUtc == null && c.RevogadoEmUtc == null && c.ExpiraEmUtc > agoraUtc)
            .OrderByDescending(c => c.CriadoEmUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(ConvitePropriedade convite, CancellationToken cancellationToken) =>
        await _db.ConvitesPropriedade.AddAsync(convite, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
