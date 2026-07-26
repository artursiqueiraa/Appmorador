using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class StatusCentralJflRepositorio : IStatusCentralJflRepositorio
{
    private readonly AppDbContext _db;

    public StatusCentralJflRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<StatusCentralJfl?> GetByEquipamentoIdAsync(Guid equipamentoId, CancellationToken cancellationToken) =>
        _db.StatusCentraisJfl.FirstOrDefaultAsync(s => s.EquipamentoId == equipamentoId, cancellationToken);

    public async Task<IReadOnlyList<StatusCentralJfl>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.StatusCentraisJfl
            .Where(s => s.Equipamento!.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(StatusCentralJfl status, CancellationToken cancellationToken)
    {
        var existente = await _db.StatusCentraisJfl
            .FirstOrDefaultAsync(s => s.EquipamentoId == status.EquipamentoId, cancellationToken)
            .ConfigureAwait(false);

        if (existente is null)
        {
            await _db.StatusCentraisJfl.AddAsync(status, cancellationToken).ConfigureAwait(false);
            return;
        }

        existente.CapturadoEmUtc = status.CapturadoEmUtc;
        existente.QuantidadeParticoesArmadas = status.QuantidadeParticoesArmadas;
        existente.QuantidadeParticoesDesarmadas = status.QuantidadeParticoesDesarmadas;
        existente.TemProblemaAtivo = status.TemProblemaAtivo;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
