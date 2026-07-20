using AppMorador.Jfl.Server;

namespace AppMorador.Api.Hosting;

/// <summary>Inicia e encerra o servidor TCP JFL junto com o ciclo de vida da aplicacao ASP.NET Core.</summary>
public sealed class JflServerHostedService : IHostedService
{
    private readonly JflTcpServer _server;
    private readonly ILogger<JflServerHostedService> _logger;

    public JflServerHostedService(JflTcpServer server, ILogger<JflServerHostedService> logger)
    {
        _server = server;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // O servidor JFL e um servico secundario: uma falha aqui (ex.: porta ja em uso)
        // nunca pode impedir a Api de subir. IHostedService.StartAsync normalmente
        // aborta o host inteiro se lancar — por isso a excecao e capturada e so
        // registrada, nunca propagada. O detalhe do erro ja foi logado por
        // JflTcpServer.Start(); aqui so confirmamos que a Api segue disponivel mesmo
        // sem o listener JFL.
        try
        {
            _server.Start();
            _logger.LogInformation("JflServerHostedService iniciado (servidor TCP JFL na porta {Port})", _server.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "JflServerHostedService nao conseguiu iniciar o servidor TCP JFL — a Api continua subindo " +
                "normalmente, mas eventos de centrais de alarme nao serao recebidos ate o problema ser corrigido.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => _server.StopAsync();
}
