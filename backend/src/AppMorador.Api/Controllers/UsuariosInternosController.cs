using AppMorador.Api.Auth;
using AppMorador.Application.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Sprint 21 (ADR 0021) — gestão das contas internas (Master/Tecnico/Suporte). Exclusivo de Master.</summary>
[ApiController]
[Route("api/usuarios-internos")]
[Authorize(Policy = Policies.RequerMaster)]
public sealed class UsuariosInternosController : ControllerBase
{
    private readonly IUsuarioInternoServico _usuarioInternoServico;

    public UsuariosInternosController(IUsuarioInternoServico usuarioInternoServico)
    {
        _usuarioInternoServico = usuarioInternoServico;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CriarUsuarioInternoRequest request, CancellationToken cancellationToken)
    {
        var result = await _usuarioInternoServico.CriarAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Conflict(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _usuarioInternoServico.ListarAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/desativar")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _usuarioInternoServico.DesativarAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
