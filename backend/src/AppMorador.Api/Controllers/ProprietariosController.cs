using AppMorador.Api.Auth;
using AppMorador.Application.Painel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 22A (ADR 0029) — leitura global de clientes para o Painel Web. Master/Suporte-only
/// (Técnico não gerencia clientes, só equipamentos/instalações — ver tabela de papéis da missão
/// Sprint 21). Achado da Fase 0: nenhum endpoint cross-tenant existia antes desta Sprint.
/// </summary>
[ApiController]
[Route("api/proprietarios")]
[Authorize(Policy = Policies.RequerSuporte)]
public sealed class ProprietariosController : ControllerBase
{
    private readonly IProprietarioServico _proprietarioServico;

    public ProprietariosController(IProprietarioServico proprietarioServico)
    {
        _proprietarioServico = proprietarioServico;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int pagina, [FromQuery] int tamanhoPagina, [FromQuery] string? busca, CancellationToken cancellationToken)
    {
        var resultado = await _proprietarioServico.ListarAsync(pagina, tamanhoPagina, busca, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _proprietarioServico.ObterDetalheAsync(id, cancellationToken);
        if (!resultado.Success)
        {
            return NotFound(new { error = resultado.Error });
        }

        return Ok(resultado.Data);
    }
}
