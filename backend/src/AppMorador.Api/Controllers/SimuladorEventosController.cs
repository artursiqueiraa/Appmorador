using AppMorador.Api.Auth;
using AppMorador.Application.Painel.Simulador;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Sprint 24 (ADR 0040) — simulador de eventos para homologação de alertas (só Master).</summary>
[ApiController]
[Route("api/painel/simulador")]
[Authorize(Policy = Policies.RequerMaster)]
public sealed class SimuladorEventosController : ControllerBase
{
    private readonly ISimuladorEventosServico _servico;

    public SimuladorEventosController(ISimuladorEventosServico servico)
    {
        _servico = servico;
    }

    [HttpPost("propriedades/{propriedadeId:guid}/eventos")]
    public async Task<IActionResult> Simular(Guid propriedadeId, [FromBody] SimularEventoRequest request, CancellationToken cancellationToken)
    {
        var result = await _servico.SimularAsync(
            User.GetUsuarioId(), propriedadeId, request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }
}
