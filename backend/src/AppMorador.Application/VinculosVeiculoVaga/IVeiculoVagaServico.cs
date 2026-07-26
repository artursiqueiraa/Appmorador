using AppMorador.Application.Common;

namespace AppMorador.Application.VinculosVeiculoVaga;

public interface IVeiculoVagaServico
{
    /// <summary>Vincula (ou realoca, se já houver vínculo ativo — encerra o antigo e cria um novo) um Veículo a uma Vaga.</summary>
    Task<Result<VinculoVeiculoVagaResponse>> VincularAsync(Guid proprietarioId, Guid veiculoId, VincularVeiculoVagaRequest request, CancellationToken cancellationToken);

    Task<Result> DesvincularAsync(Guid proprietarioId, Guid veiculoId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VinculoVeiculoVagaResponse>>> ListHistoricoByVeiculoAsync(Guid proprietarioId, Guid veiculoId, CancellationToken cancellationToken);
}
