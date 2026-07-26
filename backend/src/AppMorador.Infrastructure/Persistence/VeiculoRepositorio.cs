using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class VeiculoRepositorio : IVeiculoRepositorio
{
    private readonly AppDbContext _db;

    public VeiculoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Veiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Veiculos
            .Include(v => v.Morador)
            .ThenInclude(m => m!.Unidade)
            .ThenInclude(u => u!.Propriedade)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Veiculo?> GetByPlacaAsync(string placaNormalizada, CancellationToken cancellationToken) =>
        _db.Veiculos.FirstOrDefaultAsync(v => v.Placa == placaNormalizada, cancellationToken);

    // Sem AsNoTracking em nenhum metodo abaixo: usados tanto para exibicao/Dashboard
    // quanto pelo cascade de exclusao (Unidade/Propriedade), que precisa rastrear e
    // salvar as mudancas.
    public async Task<IReadOnlyList<Veiculo>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken) =>
        await _db.Veiculos
            .Where(v => v.MoradorId == moradorId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Veiculo>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken) =>
        await _db.Veiculos
            .Where(v => v.Morador!.UnidadeId == unidadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Veiculo>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Veiculos
            .Where(v => v.Morador!.Unidade!.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusVeiculo? status, CancellationToken cancellationToken)
    {
        var query = _db.Veiculos.Where(v => v.Morador!.Unidade!.PropriedadeId == propriedadeId);
        if (status is not null)
        {
            query = query.Where(v => v.Status == status);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Veiculo veiculo, CancellationToken cancellationToken) =>
        await _db.Veiculos.AddAsync(veiculo, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
