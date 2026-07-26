using AppMorador.Application.Common;

namespace AppMorador.Application.Veiculos;

public interface IVeiculoServico
{
    Task<Result<VeiculoResponse>> CreateAsync(Guid proprietarioId, Guid moradorId, CriarVeiculoRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VeiculoResponse>>> ListByMoradorAsync(Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken);

    Task<Result<VeiculoResponse>> UpdateAsync(Guid proprietarioId, Guid veiculoId, AtualizarVeiculoRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid veiculoId, CancellationToken cancellationToken);
}
