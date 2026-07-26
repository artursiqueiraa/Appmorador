using AppMorador.Api.Auth;
using AppMorador.Application.Intelbras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Ações de comunicação real com uma central Intelbras já cadastrada como
/// Equipamento (Fabricante=Intelbras, ver <see cref="EquipamentosController"/> para
/// o CRUD). Sprint 15 (ADR 0018) — prova de extensibilidade da arquitetura: mesma
/// forma de <see cref="CentraisJflController"/>, um Controller inteiramente novo,
/// zero linha alterada em EquipamentosController/CentraisJflController.
/// </summary>
[ApiController]
[Authorize]
public sealed class CentraisIntelbrasController : ControllerBase
{
    private readonly IIntelbrasComandoServico _intelbrasComandoServico;

    public CentraisIntelbrasController(IIntelbrasComandoServico intelbrasComandoServico)
    {
        _intelbrasComandoServico = intelbrasComandoServico;
    }

    [HttpGet("api/equipamentos/{id:guid}/intelbras")]
    public async Task<IActionResult> ObterDetalhes(Guid id, CancellationToken cancellationToken)
    {
        var result = await _intelbrasComandoServico.ObterDetalhesAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/intelbras/testar-conexao")]
    public async Task<IActionResult> TestarConexao(Guid id, CancellationToken cancellationToken)
    {
        var result = await _intelbrasComandoServico.TestarConexaoAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("api/equipamentos/{id:guid}/intelbras/status")]
    public async Task<IActionResult> ConsultarStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _intelbrasComandoServico.ConsultarStatusAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/intelbras/armar")]
    public async Task<IActionResult> Armar(Guid id, [FromBody] ParticaoIntelbrasRequest request, CancellationToken cancellationToken)
    {
        var result = await _intelbrasComandoServico.ArmarAsync(User.GetUsuarioId(), id, request.Particao, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/intelbras/desarmar")]
    public async Task<IActionResult> Desarmar(Guid id, [FromBody] ParticaoIntelbrasRequest request, CancellationToken cancellationToken)
    {
        var result = await _intelbrasComandoServico.DesarmarAsync(User.GetUsuarioId(), id, request.Particao, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/intelbras/eventos/importar")]
    public async Task<IActionResult> ImportarEventos(Guid id, CancellationToken cancellationToken)
    {
        var result = await _intelbrasComandoServico.ImportarEventosAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
