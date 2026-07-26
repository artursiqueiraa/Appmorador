using AppMorador.Api.Auth;
using AppMorador.Application.Jfl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Ações de comunicação real com uma central JFL Active 100 Bus já cadastrada como
/// Equipamento (Fabricante=Jfl, ver <see cref="EquipamentosController"/> para o
/// CRUD). Todas as rotas usam o Id do Equipamento — nunca o Número de Série
/// diretamente (detalhe interno, resolvido por <see cref="IJflComandoServico"/>).
/// </summary>
[ApiController]
[Authorize]
public sealed class CentraisJflController : ControllerBase
{
    private readonly IJflComandoServico _jflComandoServico;

    public CentraisJflController(IJflComandoServico jflComandoServico)
    {
        _jflComandoServico = jflComandoServico;
    }

    [HttpGet("api/equipamentos/{id:guid}/jfl")]
    public async Task<IActionResult> ObterDetalhes(Guid id, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.ObterDetalhesAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/testar-conexao")]
    public async Task<IActionResult> TestarConexao(Guid id, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.TestarConexaoAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("api/equipamentos/{id:guid}/jfl/status")]
    public async Task<IActionResult> ConsultarStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.ConsultarStatusAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/armar")]
    public async Task<IActionResult> Armar(Guid id, [FromBody] ParticaoRequest request, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.ArmarAsync(User.GetUsuarioId(), id, request.Particao, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/desarmar")]
    public async Task<IActionResult> Desarmar(Guid id, [FromBody] ParticaoRequest request, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.DesarmarAsync(User.GetUsuarioId(), id, request.Particao, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/armar-stay")]
    public async Task<IActionResult> ArmarStay(Guid id, [FromBody] ParticaoRequest request, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.ArmarStayAsync(User.GetUsuarioId(), id, request.Particao, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/armar-away")]
    public async Task<IActionResult> ArmarAway(Guid id, [FromBody] ParticaoRequest request, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.ArmarAwayAsync(User.GetUsuarioId(), id, request.Particao, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/pgm/acionar")]
    public async Task<IActionResult> AcionarPgm(Guid id, [FromBody] PgmRequest request, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.AcionarPgmAsync(User.GetUsuarioId(), id, request.PgmNumero, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/pgm/desligar")]
    public async Task<IActionResult> DesligarPgm(Guid id, [FromBody] PgmRequest request, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.DesligarPgmAsync(User.GetUsuarioId(), id, request.PgmNumero, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/zonas/inibir")]
    public async Task<IActionResult> InibirZona(Guid id, [FromBody] ZonaRequest request, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.InibirZonaAsync(User.GetUsuarioId(), id, request.ZonaNumero, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/jfl/zonas/desinibir")]
    public async Task<IActionResult> DesinibirZona(Guid id, [FromBody] ZonaRequest request, CancellationToken cancellationToken)
    {
        var result = await _jflComandoServico.DesinibirZonaAsync(User.GetUsuarioId(), id, request.ZonaNumero, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
