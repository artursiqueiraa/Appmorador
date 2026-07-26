using AppMorador.Api.Auth;
using AppMorador.Application.Eventos;
using AppMorador.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

[ApiController]
[Route("api/properties/{propriedadeId:guid}/eventos")]
[Authorize]
public sealed class EventosController : ControllerBase
{
    private const int TamanhoPaginaMaximo = 100;

    private readonly IEventosServico _eventosServico;

    public EventosController(IEventosServico eventosServico)
    {
        _eventosServico = eventosServico;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        Guid propriedadeId,
        [FromQuery] int pagina,
        [FromQuery] int tamanhoPagina,
        [FromQuery] string? busca,
        [FromQuery] DateTime? desdeUtc,
        [FromQuery] DateTime? ateUtc,
        [FromQuery] Guid? equipamentoId,
        [FromQuery] FabricanteEquipamento? fabricante,
        [FromQuery] OrigemEvento? origem,
        [FromQuery] CategoriaEvento? categoria,
        [FromQuery] SeveridadeEvento? severidade,
        CancellationToken cancellationToken)
    {
        var paginaValida = Math.Max(1, pagina == 0 ? 1 : pagina);
        var tamanhoPaginaValido = Math.Clamp(tamanhoPagina == 0 ? 20 : tamanhoPagina, 1, TamanhoPaginaMaximo);

        var filtro = new FiltroEventos
        {
            Busca = busca,
            DesdeUtc = desdeUtc,
            AteUtc = ateUtc,
            EquipamentoId = equipamentoId,
            Fabricante = fabricante,
            Origem = origem,
            Categoria = categoria,
            Severidade = severidade,
        };

        var result = await _eventosServico.GetEventosAsync(
            User.GetUsuarioId(), propriedadeId, filtro, paginaValida, tamanhoPaginaValido, cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}