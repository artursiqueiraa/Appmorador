using System.Diagnostics;
using AppMorador.Api.Auth;

namespace AppMorador.Api.Middleware;

/// <summary>
/// Sprint 22B (ADR 0031) — enriquece o escopo de log com o usuário autenticado, para
/// correlacionar toda a trilha de log de uma requisição ao operador que a fez (ex.: ações de
/// provisionamento/diagnóstico no Painel Web) sem precisar que cada Controller/Servico passe
/// UsuarioId manualmente para o logger. Só pode rodar depois de UseAuthentication/
/// UseAuthorization — é a única forma de já ter <c>HttpContext.User</c> populado. Também é o
/// único ponto que já conhece CorrelationId (via <see cref="CorrelationIdMiddleware"/>),
/// RequestId e UsuarioId ao mesmo tempo — por isso é aqui que a linha de log de conclusão da
/// requisição é emitida, com todos os campos como parâmetros estruturados da mensagem (nunca
/// dependendo só de `BeginScope`, cujo estado um `Dictionary` não imprime de forma legível no
/// formatter de console simples).
/// </summary>
public sealed class UsuarioLogadoEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UsuarioLogadoEnrichmentMiddleware> _logger;

    public UsuarioLogadoEnrichmentMiddleware(RequestDelegate next, ILogger<UsuarioLogadoEnrichmentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var autenticado = context.User.Identity?.IsAuthenticated == true;
        var usuarioId = autenticado ? context.User.GetUsuarioId().ToString() : "anonimo";

        using (_logger.BeginScope("{UsuarioId}", usuarioId))
        {
            await _next(context).ConfigureAwait(false);
        }

        var correlationId = context.Items["CorrelationId"] as string ?? "-";
        var cronometro = context.Items["RequestStopwatch"] as Stopwatch;

        _logger.LogInformation(
            "Requisição concluída {Method} {Path} -> {StatusCode} | CorrelationId={CorrelationId} RequestId={RequestId} UsuarioId={UsuarioId} TempoExecucaoMs={TempoExecucaoMs}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode,
            correlationId, context.TraceIdentifier, usuarioId, cronometro?.ElapsedMilliseconds ?? -1);
    }
}
