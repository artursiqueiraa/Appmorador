using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class ModeloEquipamentoRepositorio : IModeloEquipamentoRepositorio
{
    private readonly AppDbContext _db;

    public ModeloEquipamentoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<ModeloEquipamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.ModelosEquipamento.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<ModeloEquipamento?> GetByFabricanteENomeAsync(FabricanteEquipamento fabricante, string nome, CancellationToken cancellationToken) =>
        _db.ModelosEquipamento.FirstOrDefaultAsync(m => m.Fabricante == fabricante && m.Nome == nome, cancellationToken);

    public async Task<IReadOnlyList<ModeloEquipamento>> ListByFabricanteAsync(FabricanteEquipamento? fabricante, CancellationToken cancellationToken)
    {
        var query = _db.ModelosEquipamento.AsQueryable();
        if (fabricante is not null)
        {
            query = query.Where(m => m.Fabricante == fabricante);
        }

        return await query.OrderBy(m => m.Nome).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EquipamentoCapacidade>> ListCapacidadesAsync(Guid modeloEquipamentoId, CancellationToken cancellationToken) =>
        await _db.ModelosEquipamentoCapacidade
            .Where(c => c.ModeloEquipamentoId == modeloEquipamentoId)
            .Select(c => c.Capacidade)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task SubstituirCapacidadesAsync(
        Guid modeloEquipamentoId, IReadOnlyCollection<EquipamentoCapacidade> capacidades, CancellationToken cancellationToken)
    {
        var existentes = await _db.ModelosEquipamentoCapacidade
            .Where(c => c.ModeloEquipamentoId == modeloEquipamentoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _db.ModelosEquipamentoCapacidade.RemoveRange(existentes);
        _db.ModelosEquipamentoCapacidade.AddRange(capacidades.Distinct().Select(c => new ModeloEquipamentoCapacidade
        {
            Id = Guid.NewGuid(),
            ModeloEquipamentoId = modeloEquipamentoId,
            Capacidade = c,
        }));
    }

    public async Task AddAsync(ModeloEquipamento modelo, CancellationToken cancellationToken) =>
        await _db.ModelosEquipamento.AddAsync(modelo, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
