using AppMorador.Application.Common;

namespace AppMorador.Application.Equipamentos;

public interface IEquipamentoServico
{
    Task<Result<EquipamentoResponse>> CreateAsync(Guid proprietarioId, Guid propriedadeId, CriarEquipamentoRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<EquipamentoResponse>>> ListByPropriedadeAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<EquipamentoResponse>> GetByIdAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<EquipamentoResponse>> UpdateAsync(Guid proprietarioId, Guid equipamentoId, AtualizarEquipamentoRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);
}
