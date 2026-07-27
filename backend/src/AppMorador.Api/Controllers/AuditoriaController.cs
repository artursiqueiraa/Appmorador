using AppMorador.Api.Auth;
using AppMorador.Application.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 21 (ADR 0021, Fase 4) — leitura da trilha de auditoria. Master ∪
/// Suporte (Técnico não vê auditoria/logs de outros, ver tabela de papéis da
/// missão).
/// </summary>
[ApiController]
[Route("api/auditoria")]
[Authorize(Policy = Policies.RequerSuporte)]
public sealed class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaService _auditoriaService;

    public AuditoriaController(IAuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public async Task<IActionResult> ListarRecentes([FromQuery] int quantidade, CancellationToken cancellationToken)
    {
        var resultado = await _auditoriaService.ListarRecentesAsync(quantidade <= 0 ? 50 : quantidade, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("usuarios/{usuarioId:guid}")]
    public async Task<IActionResult> ListarPorUsuario(
        Guid usuarioId, [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken cancellationToken)
    {
        var resultado = await _auditoriaService.ListarPorUsuarioAsync(usuarioId, inicio, fim, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("propriedades/{propriedadeId:guid}")]
    public async Task<IActionResult> ListarPorPropriedade(
        Guid propriedadeId, [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken cancellationToken)
    {
        var resultado = await _auditoriaService.ListarPorPropriedadeAsync(propriedadeId, inicio, fim, cancellationToken);
        return Ok(resultado);
    }
}
