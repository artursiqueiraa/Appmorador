using AppMorador.Api.Auth;
using AppMorador.Application.Painel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 22A (ADR 0029) — agregado cross-propriedade para o Dashboard Operacional/Técnico do
/// Painel Web. RequerInterno (não RequerSuporte): são só contadores agregados, sem nenhum dado
/// específico de cliente (nome/e-mail) — Técnico também usa isto na Dashboard Técnico (Fase 4).
/// </summary>
[ApiController]
[Route("api/dashboard-operacional")]
[Authorize(Policy = Policies.RequerInterno)]
public sealed class DashboardOperacionalController : ControllerBase
{
    private readonly IDashboardOperacionalServico _dashboardOperacionalServico;

    public DashboardOperacionalController(IDashboardOperacionalServico dashboardOperacionalServico)
    {
        _dashboardOperacionalServico = dashboardOperacionalServico;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var resultado = await _dashboardOperacionalServico.ObterAsync(cancellationToken);
        return Ok(resultado);
    }
}
