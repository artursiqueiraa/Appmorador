using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class SnapshotOperacionalRepositorio : ISnapshotOperacionalRepositorio
{
    private readonly AppDbContext _db;

    public SnapshotOperacionalRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<SnapshotOperacional?> GetByPropriedadeIdAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.SnapshotsOperacionais.FirstOrDefaultAsync(s => s.PropriedadeId == propriedadeId, cancellationToken);

    public async Task UpsertAsync(SnapshotOperacional snapshot, CancellationToken cancellationToken)
    {
        var existente = await _db.SnapshotsOperacionais
            .FirstOrDefaultAsync(s => s.PropriedadeId == snapshot.PropriedadeId, cancellationToken)
            .ConfigureAwait(false);

        if (existente is null)
        {
            await _db.SnapshotsOperacionais.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
            return;
        }

        existente.GeradoEmUtc = snapshot.GeradoEmUtc;
        existente.Saude = snapshot.Saude;
        existente.QuantidadeEquipamentosOnline = snapshot.QuantidadeEquipamentosOnline;
        existente.QuantidadeEquipamentosOffline = snapshot.QuantidadeEquipamentosOffline;
        existente.UltimaComunicacaoUtc = snapshot.UltimaComunicacaoUtc;
        existente.QuantidadeEventosHoje = snapshot.QuantidadeEventosHoje;
        existente.QuantidadeAlarmesAtivos = snapshot.QuantidadeAlarmesAtivos;
        existente.QuantidadeFalhasDetectadas = snapshot.QuantidadeFalhasDetectadas;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
