using AppMorador.Application.Common;

namespace AppMorador.Application.Painel.VinculosEquipamento;

/// <summary>
/// Sprint 22B (ADR 0031) — regras de alocação Equipamento↔Propriedade: um equipamento nunca tem
/// mais de um vínculo ativo simultâneo; toda troca encerra o vínculo anterior, registra
/// histórico e cria um novo (nunca edita um vínculo existente). Master/Técnico-only.
/// </summary>
public interface IVinculoEquipamentoServico
{
    Task<VinculosPaginadosResponse> ListarAtivosAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VinculoResponse>>> ListarHistoricoAsync(Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<VinculoResponse>> ProvisionarAsync(
        Guid usuarioId, ProvisionarEquipamentoRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<Result<VinculoResponse>> TrocarAsync(
        Guid usuarioId, TrocarEquipamentoRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<Result> DesvincularAsync(Guid usuarioId, Guid equipamentoId, string? ipAddress, CancellationToken cancellationToken);

    Task<DashboardAlocacaoResponse> ObterDashboardAsync(CancellationToken cancellationToken);
}
