using AppMorador.Api.Auth;
using AppMorador.Application.Saude;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Sprint 27 (ADR 0043) — saúde do sistema.</summary>
[ApiController]
public sealed class SaudeController : ControllerBase
{
    private readonly ISaudeSistemaServico _saude;

    public SaudeController(ISaudeSistemaServico saude)
    {
        _saude = saude;
    }

    /// <summary>
    /// Público e sem detalhes, para monitor externo: 200 quando nada está fora, 503 quando algo está.
    /// "Atenção" (ex.: backup atrasado) não derruba o 200.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var saude = await _saude.VerificarAsync(cancellationToken);
        var corpo = new { status = saude.Geral.ToString(), verificadoEmUtc = saude.VerificadoEmUtc };
        return saude.Geral == SituacaoComponente.Fora ? StatusCode(StatusCodes.Status503ServiceUnavailable, corpo) : Ok(corpo);
    }

    /// <summary>Detalhe por componente, último backup e último teste de restauração (painel admin).</summary>
    [HttpGet("api/painel/saude")]
    [Authorize(Policy = Policies.RequerInterno)]
    public async Task<IActionResult> Detalhe(CancellationToken cancellationToken) => Ok(await _saude.VerificarAsync(cancellationToken));
}
