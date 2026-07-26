using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class CentralRepositorio : ICentralRepositorio
{
    private readonly AppDbContext _db;

    public CentralRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Central?> GetByPropriedadeIdENumeroSerieAsync(Guid propriedadeId, string numeroSerie, CancellationToken cancellationToken) =>
        _db.Centrais.FirstOrDefaultAsync(c => c.PropriedadeId == propriedadeId && c.NumeroSerie == numeroSerie, cancellationToken);
}
