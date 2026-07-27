using AppMorador.Application.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace AppMorador.Api.Auth;

/// <summary>
/// Sprint 21 (ADR 0021, Fase 4.2) — ponto ÚNICO de registro de "Falha de
/// autorização" (nunca espalhado em cada Controller/action). Decora o handler
/// padrão do ASP.NET Core: deixa toda a decisão de autorização em si intacta, só
/// observa o resultado e audita quando nega.
/// </summary>
public sealed class AuditoriaAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _handlerPadrao = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded)
        {
            var auditoria = context.RequestServices.GetRequiredService<IAuditoriaService>();
            var usuarioId = context.User.Identity?.IsAuthenticated == true ? context.User.GetUsuarioId() : (Guid?)null;
            var ip = context.Connection.RemoteIpAddress?.ToString();

            await auditoria
                .RegistrarFalhaAutorizacaoAsync(usuarioId, context.Request.Path, ip, context.RequestAborted)
                .ConfigureAwait(false);
        }

        await _handlerPadrao.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);
    }
}
