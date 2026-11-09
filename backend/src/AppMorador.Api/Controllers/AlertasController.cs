using AppMorador.Api.Auth;
using AppMorador.Application.Alertas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 24 (ADR 0040) — alertas críticos. Listagem e pânico aninhados sob a Propriedade (mesmo
/// padrão de Câmeras/Equipamentos); ver e confirmar usam só o Id do alerta (é o que chega no push).
/// </summary>
[ApiController]
[Authorize]
public sealed class AlertasController : ControllerBase
{
    private readonly IAlertaCriticoServico _servico;

    public AlertasController(IAlertaCriticoServico servico)
    {
        _servico = servico;
    }

    [HttpGet("api/properties/{propriedadeId:guid}/alertas")]
    public async Task<IActionResult> Listar(Guid propriedadeId, [FromQuery] bool apenasAbertos, CancellationToken cancellationToken)
    {
        var result = await _servico.ListarAsync(User.GetUsuarioId(), propriedadeId, apenasAbertos, cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpGet("api/alertas/{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var result = await _servico.ObterAsync(User.GetUsuarioId(), id, cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    /// <summary>"Estou ciente" — idempotente: confirmar de novo devolve o mesmo resultado.</summary>
    [HttpPost("api/alertas/{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _servico.ConfirmarAsync(User.GetUsuarioId(), id, cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    /// <summary>Botão de pânico do app (o app exige segurar 3 s antes de chamar).</summary>
    [HttpPost("api/properties/{propriedadeId:guid}/panico")]
    public async Task<IActionResult> Panico(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _servico.AcionarPanicoAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        return result.Success ? StatusCode(StatusCodes.Status201Created, result.Data) : NotFound(new { error = result.Error });
    }

    [HttpGet("api/properties/{propriedadeId:guid}/alertas/configuracao")]
    public async Task<IActionResult> ObterConfiguracao(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _servico.ObterConfiguracaoAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPut("api/properties/{propriedadeId:guid}/alertas/configuracao")]
    public async Task<IActionResult> AtualizarConfiguracao(
        Guid propriedadeId, [FromBody] AtualizarConfiguracaoAlertasRequest request, CancellationToken cancellationToken)
    {
        var result = await _servico.AtualizarConfiguracaoAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Data);
        }

        return result.Error == "Propriedade não encontrada." ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
    }
}
