using AppMorador.Application.Common;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Equipamentos;

/// <summary>Sprint 21 (ADR 0027) — catálogo de modelos + suas capacidades. Exclusivo de Técnico/Master (quem instala/configura hardware).</summary>
public interface IModeloEquipamentoServico
{
    Task<ModeloEquipamentoResponse> CriarAsync(CriarModeloEquipamentoRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ModeloEquipamentoResponse>> ListarAsync(FabricanteEquipamento? fabricante, CancellationToken cancellationToken);

    Task<Result<ModeloEquipamentoResponse>> DefinirCapacidadesAsync(
        Guid modeloEquipamentoId, IReadOnlyCollection<EquipamentoCapacidade> capacidades, CancellationToken cancellationToken);
}
