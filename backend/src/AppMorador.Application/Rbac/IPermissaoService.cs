using AppMorador.Domain.Entities;

namespace AppMorador.Application.Rbac;

/// <summary>
/// Sprint 21 (ADR 0025/0026/0027) — ponto único de consulta de "o que este usuário
/// pode fazer nesta propriedade" / "o que esta propriedade contratou" / "o que este
/// equipamento suporta". Controllers chamam isto explicitamente (ver Fase 5.2/5.3 da
/// missão) — a Policy de rota (Master/Interno/Cliente) resolve o papel GROSSEIRO,
/// este serviço resolve o refinamento por recurso específico.
/// </summary>
public interface IPermissaoService
{
    Task<bool> TemPermissaoAsync(Guid usuarioId, Guid propriedadeId, PermissaoFuncionalidade permissao, CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissaoFuncionalidade>> ListarPermissoesAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<bool> PropriedadeTemFeatureAsync(Guid propriedadeId, FeatureFlag feature, CancellationToken cancellationToken);

    Task<IReadOnlyList<FeatureFlag>> ListarFeaturesAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EquipamentoCapacidade>> ListarCapacidadesAsync(Guid equipamentoId, CancellationToken cancellationToken);
}
