using AppMorador.Api.Auth;
using AppMorador.Application.ContatosEmergencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Sprint 24 (ADR 0040) — contatos de emergência da propriedade (só o dono gerencia).</summary>
[ApiController]
[Authorize]
public sealed class ContatosEmergenciaController : ControllerBase
{
    private const string NaoEncontrada = "Propriedade não encontrada.";
    private const string ContatoNaoEncontrado = "Contato não encontrado.";

    private readonly IContatoEmergenciaServico _servico;

    public ContatosEmergenciaController(IContatoEmergenciaServico servico)
    {
        _servico = servico;
    }

    [HttpGet("api/properties/{propriedadeId:guid}/contatos-emergencia")]
    public async Task<IActionResult> Listar(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _servico.ListarAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost("api/properties/{propriedadeId:guid}/contatos-emergencia")]
    public async Task<IActionResult> Criar(Guid propriedadeId, [FromBody] SalvarContatoEmergenciaRequest request, CancellationToken cancellationToken)
    {
        var result = await _servico.CriarAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (result.Success)
        {
            return StatusCode(StatusCodes.Status201Created, result.Data);
        }

        return result.Error == NaoEncontrada ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("api/properties/{propriedadeId:guid}/contatos-emergencia/ordem")]
    public async Task<IActionResult> Reordenar(
        Guid propriedadeId, [FromBody] ReordenarContatosEmergenciaRequest request, CancellationToken cancellationToken)
    {
        var result = await _servico.ReordenarAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Data);
        }

        return result.Error == NaoEncontrada ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("api/contatos-emergencia/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarContatoEmergenciaRequest request, CancellationToken cancellationToken)
    {
        var result = await _servico.AtualizarAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Data);
        }

        return result.Error == ContatoNaoEncontrado ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("api/contatos-emergencia/{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        var result = await _servico.RemoverAsync(User.GetUsuarioId(), id, cancellationToken);
        return result.Success ? NoContent() : NotFound(new { error = result.Error });
    }
}
