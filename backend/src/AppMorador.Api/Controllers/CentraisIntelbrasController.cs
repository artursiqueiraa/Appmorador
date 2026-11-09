using AppMorador.Api.Auth;
using AppMorador.Api.Comandos;
using AppMorador.Application.Comandos;
using AppMorador.Domain.Entities;
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
    private readonly IExecucaoComandoServico _execucao;

    public CentraisIntelbrasController(IIntelbrasComandoServico intelbrasComandoServico, IExecucaoComandoServico execucao)
    {
        _intelbrasComandoServico = intelbrasComandoServico;
        _execucao = execucao;
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
    public async Task<IActionResult> Armar(Guid id, [FromBody] ParticaoIntelbrasRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.Armar, equipamentoId: id, parametros: $"partição {request.Particao}"),
            ct => _intelbrasComandoServico.ArmarAsync(User.GetUsuarioId(), id, request.Particao, ct),
            r => (r.Sucesso, r.MensagemErro),
            cancellationToken));

    [HttpPost("api/equipamentos/{id:guid}/intelbras/desarmar")]
    public async Task<IActionResult> Desarmar(Guid id, [FromBody] ParticaoIntelbrasRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.Desarmar, equipamentoId: id, parametros: $"partição {request.Particao}"),
            ct => _intelbrasComandoServico.DesarmarAsync(User.GetUsuarioId(), id, request.Particao, ct),
            r => (r.Sucesso, r.MensagemErro),
            cancellationToken));

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
