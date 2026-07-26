using AppMorador.Api.Auth;
using AppMorador.Application.VinculosVeiculoVaga;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Vinculação Veículo↔Vaga — aninhado sob o Veículo. PUT vincula/realoca (mesma operação); DELETE desvincula; GET lista o histórico completo.</summary>
[ApiController]
[Authorize]
public sealed class VeiculoVagaController : ControllerBase
{
    private readonly IVeiculoVagaServico _veiculoVagaServico;

    public VeiculoVagaController(IVeiculoVagaServico veiculoVagaServico)
    {
        _veiculoVagaServico = veiculoVagaServico;
    }

    [HttpPut("api/veiculos/{veiculoId:guid}/vinculo")]
    public async Task<IActionResult> Vincular(Guid veiculoId, [FromBody] VincularVeiculoVagaRequest request, CancellationToken cancellationToken)
    {
        var result = await _veiculoVagaServico.VincularAsync(User.GetUsuarioId(), veiculoId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/veiculos/{veiculoId:guid}/vinculo")]
    public async Task<IActionResult> Desvincular(Guid veiculoId, CancellationToken cancellationToken)
    {
        var result = await _veiculoVagaServico.DesvincularAsync(User.GetUsuarioId(), veiculoId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpGet("api/veiculos/{veiculoId:guid}/vinculos")]
    public async Task<IActionResult> ListHistorico(Guid veiculoId, CancellationToken cancellationToken)
    {
        var result = await _veiculoVagaServico.ListHistoricoByVeiculoAsync(User.GetUsuarioId(), veiculoId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
