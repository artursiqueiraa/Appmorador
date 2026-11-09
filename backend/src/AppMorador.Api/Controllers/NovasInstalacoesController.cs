using AppMorador.Api.Auth;
using AppMorador.Application.Painel.NovasInstalacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Akteck Seg (ADR 0035) — "Nova instalação" do Painel Web: cria a conta do cliente, a
/// primeira propriedade e (opcionalmente) vincula um equipamento livre, num envio só.
/// Exclusivo de Master. Resolve parte da DIVIDA_TECNICA 47 (criar cliente pelo Painel).
/// </summary>
[ApiController]
[Route("api/painel/novas-instalacoes")]
[Authorize(Policy = Policies.RequerMaster)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class NovasInstalacoesController : ControllerBase
{
    private readonly INovaInstalacaoServico _novaInstalacaoServico;

    public NovasInstalacoesController(INovaInstalacaoServico novaInstalacaoServico)
    {
        _novaInstalacaoServico = novaInstalacaoServico;
    }

    /// <summary>Cria cliente + propriedade (+ vínculo de equipamento, se informado).</summary>
    /// <response code="201">Instalação criada.</response>
    /// <response code="400">Dados inválidos (ex.: senha fora da regra).</response>
    /// <response code="409">E-mail já em uso, ou equipamento inexistente/já provisionado.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar([FromBody] CriarNovaInstalacaoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _novaInstalacaoServico.CriarAsync(
            User.GetUsuarioId(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        if (!resultado.Success)
        {
            return Conflict(new { error = resultado.Error });
        }
        return StatusCode(StatusCodes.Status201Created, resultado.Data);
    }
}
