using AppMorador.Api.Auth;
using AppMorador.Application.Notificacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 19 (ADR 0023) — ciclo de vida do token de push do dispositivo do próprio
/// usuário autenticado (nunca aninhado sob Propriedade — um dispositivo pode
/// existir antes de qualquer Propriedade ser selecionada).
/// </summary>
[ApiController]
[Authorize]
public sealed class DispositivosPushController : ControllerBase
{
    private readonly IDispositivoPushServico _servico;

    public DispositivosPushController(IDispositivoPushServico servico)
    {
        _servico = servico;
    }

    [HttpPost("api/dispositivos-push")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarDispositivoPushRequest request, CancellationToken cancellationToken)
    {
        var response = await _servico.RegistrarAsync(User.GetUsuarioId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("api/dispositivos-push/{id:guid}")]
    public async Task<IActionResult> AtualizarToken(Guid id, [FromBody] AtualizarDispositivoPushRequest request, CancellationToken cancellationToken)
    {
        var result = await _servico.AtualizarTokenAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/dispositivos-push/{id:guid}/preferencias")]
    public async Task<IActionResult> AtualizarPreferencias(Guid id, [FromBody] AtualizarPreferenciasDispositivoPushRequest request, CancellationToken cancellationToken)
    {
        var result = await _servico.AtualizarPreferenciasAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Sprint 19 — desativa por Id (não por token na URL, como a missão original
    /// sugeriu): um token pode conter caracteres não seguros para segmento de rota,
    /// e o Mobile já tem o Id devolvido pelo registro — ver ADR 0023 para o racional
    /// completo desta escolha.
    /// </summary>
    [HttpDelete("api/dispositivos-push/{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken cancellationToken)
    {
        await _servico.DesativarAsync(User.GetUsuarioId(), id, cancellationToken);
        return NoContent();
    }
}
