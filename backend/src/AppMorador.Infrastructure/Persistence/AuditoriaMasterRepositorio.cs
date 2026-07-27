using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class AuditoriaMasterRepositorio : IAuditoriaMasterRepositorio
{
    private readonly AppDbContext _db;

    public AuditoriaMasterRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AuditoriaMaster registro, CancellationToken cancellationToken) =>
        await _db.AuditoriaMaster.AddAsync(registro, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<AuditoriaMaster>> ListByUsuarioAsync(
        Guid usuarioId, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken)
    {
        var query = _db.AuditoriaMaster.Where(a => a.UsuarioId == usuarioId);
        query = AplicarFiltroPeriodo(query, inicio, fim);
        return await query.OrderByDescending(a => a.DataHoraUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditoriaMaster>> ListByPropriedadeAsync(
        Guid propriedadeId, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken)
    {
        var propriedadeIdTexto = propriedadeId.ToString();
        var query = _db.AuditoriaMaster.Where(a => a.Entidade == "Propriedade" && a.EntidadeId == propriedadeIdTexto);
        query = AplicarFiltroPeriodo(query, inicio, fim);
        return await query.OrderByDescending(a => a.DataHoraUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditoriaMaster>> ListRecentesAsync(int quantidade, CancellationToken cancellationToken) =>
        await _db.AuditoriaMaster
            .OrderByDescending(a => a.DataHoraUtc)
            .Take(quantidade)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    private static IQueryable<AuditoriaMaster> AplicarFiltroPeriodo(IQueryable<AuditoriaMaster> query, DateTime? inicio, DateTime? fim)
    {
        if (inicio is not null)
        {
            query = query.Where(a => a.DataHoraUtc >= inicio);
        }

        if (fim is not null)
        {
            query = query.Where(a => a.DataHoraUtc <= fim);
        }

        return query;
    }
}
