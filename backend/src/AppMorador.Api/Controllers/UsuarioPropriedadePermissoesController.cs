using AppMorador.Api.Auth;
using AppMorador.Application.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 21 (ADR 0025) — Permissões Funcionais de um vínculo Usuario↔Propriedade.
/// Exclusivo de Técnico/Master nesta Sprint (autoatendimento pelo próprio
/// Administrador da propriedade fica para quando o Painel Web/app expuser essa
/// gestão, ver Fora de Escopo da missão).
/// </summary>
[ApiController]
[Route("api/properties/{propriedadeId:guid}/usuarios/{usuarioAlvoId:guid}/permissoes")]
[Authorize(Policy = Policies.RequerTecnico)]
public sealed class UsuarioPropriedadePermissoesController : ControllerBase
{
    private readonly IUsuarioPropriedadePermissaoServico _permissaoServico;

    public UsuarioPropriedadePermissoesController(IUsuarioPropriedadePermissaoServico permissaoServico)
    {
        _permissaoServico = permissaoServico;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid propriedadeId, Guid usuarioAlvoId, CancellationToken cancellationToken)
    {
        var result = await _permissaoServico.ListarAsync(propriedadeId, usuarioAlvoId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut]
    public async Task<IActionResult> Definir(
        Guid propriedadeId, Guid usuarioAlvoId, [FromBody] DefinirPermissoesRequest request, CancellationToken cancellationToken)
    {
        var result = await _permissaoServico.DefinirAsync(propriedadeId, usuarioAlvoId, request.Permissoes, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
