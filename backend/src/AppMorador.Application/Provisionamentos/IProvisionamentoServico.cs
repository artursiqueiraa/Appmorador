using AppMorador.Application.Common;

namespace AppMorador.Application.Provisionamentos;

/// <summary>
/// Sprint 21 (ADR 0028) — registro do "pacote de instalação" de uma Propriedade.
/// Exclusivo de Técnico/Master. Nesta Sprint é um registro de metadados
/// (Nome/Template/Status) — a árvore de equipamentos vinculados fica para uma
/// Sprint futura dedicada, ver ADR 0028.
/// </summary>
public interface IProvisionamentoServico
{
    Task<Result<ProvisionamentoResponse>> CriarAsync(Guid propriedadeId, CriarProvisionamentoRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ProvisionamentoResponse>>> ListarAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<ProvisionamentoResponse>> ArquivarAsync(Guid id, CancellationToken cancellationToken);
}
