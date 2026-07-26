using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class EventoEquipamentoRepositorio : IEventoEquipamentoRepositorio
{
    private readonly AppDbContext _db;

    public EventoEquipamentoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EventoEquipamento>> ListByEquipamentoAsync(Guid equipamentoId, CancellationToken cancellationToken) =>
        await _db.EventosEquipamento
            .Where(e => e.EquipamentoId == equipamentoId)
            .OrderByDescending(e => e.OcorridoEmUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<DateTime?> GetUltimoRecebidoAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.EventosEquipamento
            .Where(e => e.Equipamento!.PropriedadeId == propriedadeId)
            .OrderByDescending(e => e.OcorridoEmUtc)
            .Select(e => (DateTime?)e.OcorridoEmUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddRangeAsync(IReadOnlyList<EventoEquipamento> eventos, CancellationToken cancellationToken) =>
        await _db.EventosEquipamento.AddRangeAsync(eventos, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
