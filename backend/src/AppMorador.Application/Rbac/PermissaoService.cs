using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Rbac;

public sealed class PermissaoService : IPermissaoService
{
    private readonly IUsuarioPropriedadeRepositorio _usuariosPropriedade;
    private readonly IUsuarioPropriedadePermissaoRepositorio _permissoes;
    private readonly IPropriedadeFeatureFlagRepositorio _features;
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IModeloEquipamentoRepositorio _modelosEquipamento;

    public PermissaoService(
        IUsuarioPropriedadeRepositorio usuariosPropriedade,
        IUsuarioPropriedadePermissaoRepositorio permissoes,
        IPropriedadeFeatureFlagRepositorio features,
        IEquipamentoRepositorio equipamentos,
        IModeloEquipamentoRepositorio modelosEquipamento)
    {
        _usuariosPropriedade = usuariosPropriedade;
        _permissoes = permissoes;
        _features = features;
        _equipamentos = equipamentos;
        _modelosEquipamento = modelosEquipamento;
    }

    public async Task<bool> TemPermissaoAsync(Guid usuarioId, Guid propriedadeId, PermissaoFuncionalidade permissao, CancellationToken cancellationToken)
    {
        var vinculo = await _usuariosPropriedade.GetAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        if (vinculo is null || !vinculo.Ativo)
        {
            return false;
        }

        return await _permissoes.TemPermissaoAsync(vinculo.Id, permissao, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PermissaoFuncionalidade>> ListarPermissoesAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var vinculo = await _usuariosPropriedade.GetAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        if (vinculo is null || !vinculo.Ativo)
        {
            return [];
        }

        return await _permissoes.ListAsync(vinculo.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> PropriedadeTemFeatureAsync(Guid propriedadeId, FeatureFlag feature, CancellationToken cancellationToken) =>
        await _features.TemFeatureAtivaAsync(propriedadeId, feature, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<FeatureFlag>> ListarFeaturesAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _features.ListAtivasAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<EquipamentoCapacidade>> ListarCapacidadesAsync(Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento?.ModeloEquipamentoId is null)
        {
            return [];
        }

        return await _modelosEquipamento.ListCapacidadesAsync(equipamento.ModeloEquipamentoId.Value, cancellationToken).ConfigureAwait(false);
    }
}
