using AppMorador.Api.Auth;
using AppMorador.Application.Comandos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AppMorador.Api.Controllers;

/// <summary>Sprint 27 (ADR 0043) — PIN de confirmação e auditoria de comandos.</summary>
[ApiController]
[Authorize]
public sealed class SegurancaContaController : ControllerBase
{
    private readonly IPinConfirmacaoServico _pin;
    private readonly IAuditoriaComandosServico _auditoria;

    public SegurancaContaController(IPinConfirmacaoServico pin, IAuditoriaComandosServico auditoria)
    {
        _pin = pin;
        _auditoria = auditoria;
    }

    [HttpGet("api/minha-conta/pin")]
    public async Task<IActionResult> ObterPin(CancellationToken cancellationToken)
    {
        var result = await _pin.ObterAsync(User.GetUsuarioId(), cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    /// <summary>Cria, troca ou redefine ("esqueci o PIN") — sempre com a senha da conta.</summary>
    [HttpPut("api/minha-conta/pin")]
    [EnableRateLimiting(RateLimiterPolicies.Auth)]
    public async Task<IActionResult> DefinirPin([FromBody] DefinirPinRequest request, CancellationToken cancellationToken)
    {
        var result = await _pin.DefinirAsync(User.GetUsuarioId(), request, cancellationToken);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Confere o PIN sem executar nada — usado ao ligar "confirmar com digital/rosto", antes de guardar
    /// o PIN no aparelho. Conta tentativas como qualquer confirmação.
    /// </summary>
    [HttpPost("api/minha-conta/pin/verificar")]
    public async Task<IActionResult> VerificarPin([FromBody] VerificarPinRequest request, CancellationToken cancellationToken)
    {
        var r = await _pin.ValidarAsync(User.GetUsuarioId(), request.Pin, cancellationToken);
        return r.Situacao switch
        {
            SituacaoValidacaoPin.Valido => NoContent(),
            SituacaoValidacaoPin.Ausente => BadRequest(new { error = "Digite o PIN.", codigo = "PIN_OBRIGATORIO" }),
            SituacaoValidacaoPin.SemPinCadastrado => BadRequest(new { error = "Crie um PIN primeiro.", codigo = "PIN_INEXISTENTE" }),
            SituacaoValidacaoPin.Bloqueado => BadRequest(new { error = "Muitas tentativas erradas. Aguarde 15 minutos ou redefina o PIN com a senha da conta.", codigo = "PIN_BLOQUEADO" }),
            _ => BadRequest(new { error = r.TentativasRestantes == 1 ? "PIN incorreto. Resta 1 tentativa." : $"PIN incorreto. Restam {r.TentativasRestantes} tentativas.", codigo = "PIN_INCORRETO" }),
        };
    }

    [HttpGet("api/properties/{propriedadeId:guid}/comandos")]
    public async Task<IActionResult> ListarComandos(Guid propriedadeId, [FromQuery] int limite = 50, CancellationToken cancellationToken = default)
    {
        var result = await _auditoria.ListarAsync(User.GetUsuarioId(), propriedadeId, limite, cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }
}

public sealed class VerificarPinRequest
{
    public string Pin { get; init; } = string.Empty;
}
