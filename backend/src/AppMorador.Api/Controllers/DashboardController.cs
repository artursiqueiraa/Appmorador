using AppMorador.Api.Auth;
using AppMorador.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

[ApiController]
[Route("api/properties/{propriedadeId:guid}/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardServico _dashboardServico;

    public DashboardController(IDashboardServico dashboardServico)
    {
        _dashboardServico = dashboardServico;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _dashboardServico.GetAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
