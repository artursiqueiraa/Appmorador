using AppMorador.Api.Auth;
using AppMorador.Application.Autorizacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="PermissoesAcessoController"/>: Create/List aninhados sob o Visitante, Update/Status/Delete pelo Id da Autorização.</summary>
[ApiController]
[Authorize]
public sealed class AutorizacoesController : ControllerBase
{
    private readonly IAutorizacaoServico _autorizacaoServico;

    public AutorizacoesController(IAutorizacaoServico autorizacaoServico)
    {
        _autorizacaoServico = autorizacaoServico;
    }

    [HttpPost("api/visitantes/{visitanteId:guid}/autorizacoes")]
    public async Task<IActionResult> Create(Guid visitanteId, [FromBody] CriarAutorizacaoRequest request, CancellationToken cancellationToken)
    {
        var result = await _autorizacaoServico.CreateAsync(User.GetUsuarioId(), visitanteId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/visitantes/{visitanteId:guid}/autorizacoes")]
    public async Task<IActionResult> List(Guid visitanteId, CancellationToken cancellationToken)
    {
        var result = await _autorizacaoServico.ListByVisitanteAsync(User.GetUsuarioId(), visitanteId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/autorizacoes/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarAutorizacaoRequest request, CancellationToken cancellationToken)
    {
        var result = await _autorizacaoServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/autorizacoes/{id:guid}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarStatusAutorizacaoRequest request, CancellationToken cancellationToken)
    {
        var result = await _autorizacaoServico.AtualizarStatusAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/autorizacoes/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _autorizacaoServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
