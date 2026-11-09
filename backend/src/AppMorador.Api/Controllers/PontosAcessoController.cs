using AppMorador.Api.Auth;
using AppMorador.Api.Comandos;
using AppMorador.Application.Comandos;
using AppMorador.Domain.Entities;
using AppMorador.Application.PontosAcesso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrÃ£o de rota de <see cref="UnidadesController"/>: Create/List aninhados sob a Propriedade, Update/Delete pelo Id do Ponto de Acesso.</summary>
[ApiController]
[Authorize]
public sealed class PontosAcessoController : ControllerBase
{
    private readonly IPontoAcessoServico _pontoAcessoServico;
    private readonly IExecucaoComandoServico _execucao;

    public PontosAcessoController(IPontoAcessoServico pontoAcessoServico, IExecucaoComandoServico execucao)
    {
        _pontoAcessoServico = pontoAcessoServico;
        _execucao = execucao;
    }

    [HttpPost("api/properties/{propriedadeId:guid}/pontos-acesso")]
    public async Task<IActionResult> Create(Guid propriedadeId, [FromBody] CriarPontoAcessoRequest request, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.CreateAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/properties/{propriedadeId:guid}/pontos-acesso")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.ListByPropriedadeAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/pontos-acesso/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarPontoAcessoRequest request, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/pontos-acesso/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>Sprint 24.5 â€” equipamentos que podem abrir portas (formulÃ¡rio "O que abre esta porta?").</summary>
    [HttpGet("api/properties/{propriedadeId:guid}/pontos-acesso/acionadores")]
    public async Task<IActionResult> ListarAcionadores(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.ListarAcionadoresAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    /// <summary>Sprint 24.5 â€” abre a porta. 200 com Sucesso true/false sempre que o ponto existe.</summary>
    [HttpPost("api/pontos-acesso/{id:guid}/abrir")]
    public async Task<IActionResult> Abrir(Guid id, CancellationToken cancellationToken) =>
        // Sprint 27 (ADR 0043) - exige PIN (quando criado), registra em ComandosExecutados, nao repete a mesma chave.
        this.ParaResposta(await _execucao.ExecutarAsync(
            this.Solicitacao(TipoComando.AbrirAcesso, pontoAcessoId: id),
            ct => _pontoAcessoServico.AbrirAsync(User.GetUsuarioId(), id, ct),
            r => (r.Sucesso, r.Sucesso ? null : r.Mensagem),
            cancellationToken));
}
