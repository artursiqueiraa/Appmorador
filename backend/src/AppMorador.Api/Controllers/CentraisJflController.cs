using AppMorador.Api.Auth;
using AppMorador.Api.Comandos;
using AppMorador.Application.Comandos;
using AppMorador.Domain.Entities;
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
    private readonly IExecucaoComandoServico _execucao;

    public CentraisJflController(IJflComandoServico jflComandoServico, IExecucaoComandoServico execucao)
    {
        _jflComandoServico = jflComandoServico;
        _execucao = execucao;
    }

    /// <summary>Sprint 27 (ADR 0043) — o que conta como "a central obedeceu" na auditoria.</summary>
    private static (bool Sucesso, string? Mensagem) Avaliar(ResultadoComandoJfl resultado) => (resultado.Sucesso, resultado.MensagemErro);

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
    public async Task<IActionResult> Armar(Guid id, [FromBody] ParticaoRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.Armar, equipamentoId: id, parametros: $"partição {request.Particao}"),
            ct => _jflComandoServico.ArmarAsync(User.GetUsuarioId(), id, request.Particao, ct),
            Avaliar,
            cancellationToken));

    [HttpPost("api/equipamentos/{id:guid}/jfl/desarmar")]
    public async Task<IActionResult> Desarmar(Guid id, [FromBody] ParticaoRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.Desarmar, equipamentoId: id, parametros: $"partição {request.Particao}"),
            ct => _jflComandoServico.DesarmarAsync(User.GetUsuarioId(), id, request.Particao, ct),
            Avaliar,
            cancellationToken));

    [HttpPost("api/equipamentos/{id:guid}/jfl/armar-stay")]
    public async Task<IActionResult> ArmarStay(Guid id, [FromBody] ParticaoRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.ArmarStay, equipamentoId: id, parametros: $"partição {request.Particao}"),
            ct => _jflComandoServico.ArmarStayAsync(User.GetUsuarioId(), id, request.Particao, ct),
            Avaliar,
            cancellationToken));

    [HttpPost("api/equipamentos/{id:guid}/jfl/armar-away")]
    public async Task<IActionResult> ArmarAway(Guid id, [FromBody] ParticaoRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.ArmarAway, equipamentoId: id, parametros: $"partição {request.Particao}"),
            ct => _jflComandoServico.ArmarAwayAsync(User.GetUsuarioId(), id, request.Particao, ct),
            Avaliar,
            cancellationToken));

    [HttpPost("api/equipamentos/{id:guid}/jfl/pgm/acionar")]
    public async Task<IActionResult> AcionarPgm(Guid id, [FromBody] PgmRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.AcionarPgm, equipamentoId: id, parametros: $"PGM {request.PgmNumero}"),
            ct => _jflComandoServico.AcionarPgmAsync(User.GetUsuarioId(), id, request.PgmNumero, ct),
            Avaliar,
            cancellationToken));

    [HttpPost("api/equipamentos/{id:guid}/jfl/pgm/desligar")]
    public async Task<IActionResult> DesligarPgm(Guid id, [FromBody] PgmRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.DesligarPgm, equipamentoId: id, parametros: $"PGM {request.PgmNumero}"),
            ct => _jflComandoServico.DesligarPgmAsync(User.GetUsuarioId(), id, request.PgmNumero, ct),
            Avaliar,
            cancellationToken));

    [HttpPost("api/equipamentos/{id:guid}/jfl/zonas/inibir")]
    public async Task<IActionResult> InibirZona(Guid id, [FromBody] ZonaRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.InibirZona, equipamentoId: id, parametros: $"zona {request.ZonaNumero}"),
            ct => _jflComandoServico.InibirZonaAsync(User.GetUsuarioId(), id, request.ZonaNumero, ct),
            Avaliar,
            cancellationToken));

    [HttpPost("api/equipamentos/{id:guid}/jfl/zonas/desinibir")]
    public async Task<IActionResult> DesinibirZona(Guid id, [FromBody] ZonaRequest request, CancellationToken cancellationToken) =>
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.DesinibirZona, equipamentoId: id, parametros: $"zona {request.ZonaNumero}"),
            ct => _jflComandoServico.DesinibirZonaAsync(User.GetUsuarioId(), id, request.ZonaNumero, ct),
            Avaliar,
            cancellationToken));
}
