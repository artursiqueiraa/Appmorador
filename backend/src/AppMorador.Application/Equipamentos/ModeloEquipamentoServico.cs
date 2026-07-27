using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Equipamentos;

public sealed class ModeloEquipamentoServico : IModeloEquipamentoServico
{
    private readonly IModeloEquipamentoRepositorio _modelos;

    public ModeloEquipamentoServico(IModeloEquipamentoRepositorio modelos)
    {
        _modelos = modelos;
    }

    public async Task<ModeloEquipamentoResponse> CriarAsync(CriarModeloEquipamentoRequest request, CancellationToken cancellationToken)
    {
        var nome = request.Nome.Trim();
        var existente = await _modelos.GetByFabricanteENomeAsync(request.Fabricante, nome, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            return await ToDtoAsync(existente, cancellationToken).ConfigureAwait(false);
        }

        var modelo = new ModeloEquipamento { Id = Guid.NewGuid(), Fabricante = request.Fabricante, Nome = nome, CreatedAtUtc = DateTime.UtcNow };
        await _modelos.AddAsync(modelo, cancellationToken).ConfigureAwait(false);
        await _modelos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToDtoAsync(modelo, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ModeloEquipamentoResponse>> ListarAsync(FabricanteEquipamento? fabricante, CancellationToken cancellationToken)
    {
        var modelos = await _modelos.ListByFabricanteAsync(fabricante, cancellationToken).ConfigureAwait(false);
        var resultado = new List<ModeloEquipamentoResponse>(modelos.Count);
        foreach (var modelo in modelos)
        {
            resultado.Add(await ToDtoAsync(modelo, cancellationToken).ConfigureAwait(false));
        }

        return resultado;
    }

    public async Task<Result<ModeloEquipamentoResponse>> DefinirCapacidadesAsync(
        Guid modeloEquipamentoId, IReadOnlyCollection<EquipamentoCapacidade> capacidades, CancellationToken cancellationToken)
    {
        var modelo = await _modelos.GetByIdAsync(modeloEquipamentoId, cancellationToken).ConfigureAwait(false);
        if (modelo is null)
        {
            return Result<ModeloEquipamentoResponse>.Fail("Modelo de equipamento não encontrado.");
        }

        await _modelos.SubstituirCapacidadesAsync(modeloEquipamentoId, capacidades, cancellationToken).ConfigureAwait(false);
        await _modelos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ModeloEquipamentoResponse>.Ok(await ToDtoAsync(modelo, cancellationToken).ConfigureAwait(false));
    }

    private async Task<ModeloEquipamentoResponse> ToDtoAsync(ModeloEquipamento modelo, CancellationToken cancellationToken) => new()
    {
        Id = modelo.Id,
        Fabricante = modelo.Fabricante,
        Nome = modelo.Nome,
        Capacidades = await _modelos.ListCapacidadesAsync(modelo.Id, cancellationToken).ConfigureAwait(false),
    };
}
