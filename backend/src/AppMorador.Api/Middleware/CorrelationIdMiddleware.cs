using System.Diagnostics;

namespace AppMorador.Api.Middleware;

/// <summary>
/// Sprint 22B (ADR 0031) — CorrelationId por requisição, aceito via header `X-Correlation-Id`
/// (permite rastreamento de ponta a ponta quando o chamador já tiver um) ou gerado aqui.
/// RequestId usa <see cref="HttpContext.TraceIdentifier"/> (já único por requisição, gerado pelo
/// próprio Kestrel/ASP.NET Core — nunca reinventado aqui). Também mede o tempo total de execução
/// da requisição; a linha de log final (com CorrelationId/RequestId/UsuarioId/tempo, todos como
/// parâmetros estruturados — nunca dependendo de `BeginScope` sozinho, cujo estado um
/// `Dictionary` não imprime de forma legível no formatter de console simples) é emitida por
/// <see cref="UsuarioLogadoEnrichmentMiddleware"/>, o único ponto que já sabe o usuário
/// autenticado. Fica fora desse escopo de propósito: precisa envolver também erros que acontecem
/// antes da autenticação (ver ExceptionHandlingMiddleware, que roda dentro deste escopo).
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existente) && !string.IsNullOrWhiteSpace(existente)
            ? existente.ToString()
            : Guid.NewGuid().ToString("n");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope("{CorrelationId}", correlationId))
        {
            var cronometro = Stopwatch.StartNew();
            context.Items["RequestStopwatch"] = cronometro;
            await _next(context).ConfigureAwait(false);
        }
    }
}
