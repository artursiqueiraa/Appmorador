using AppMorador.Api.Auth;
using AppMorador.Application.Propriedades;
using AppMorador.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 21 (ADR 0026) — "o que a propriedade contratou". Exclusivo de
/// Técnico/Master nesta Sprint: é decisão comercial/de instalação, não do
/// cliente, e o serviço (<see cref="IPropriedadeFeatureFlagServico"/>) não faz
/// ownership check por ProprietarioId — só interno acessa por ora. Consumo pelo
/// app mobile (cliente lendo suas próprias features) é decidido junto do
/// usePermissao hook, na Fase 6 desta Sprint.
/// </summary>
[ApiController]
[Route("api/properties/{propriedadeId:guid}/features")]
[Authorize(Policy = Policies.RequerTecnico)]
public sealed class PropriedadeFeatureFlagsController : ControllerBase
{
    private readonly IPropriedadeFeatureFlagServico _featureFlagServico;

    public PropriedadeFeatureFlagsController(IPropriedadeFeatureFlagServico featureFlagServico)
    {
        _featureFlagServico = featureFlagServico;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _featureFlagServico.ListarAtivasAsync(propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("{feature}")]
    public async Task<IActionResult> Definir(Guid propriedadeId, FeatureFlag feature, [FromBody] DefinirFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var result = await _featureFlagServico.DefinirAsync(propriedadeId, feature, request.Ativo, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
