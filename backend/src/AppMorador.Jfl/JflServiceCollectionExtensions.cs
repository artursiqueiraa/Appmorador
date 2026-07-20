using AppMorador.Jfl.Server;
using AppMorador.Jfl.Server.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppMorador.Jfl;

/// <summary>
/// Registra a infraestrutura de protocolo do servidor JFL (sessao, dispatcher,
/// handshake, keep-alive) no container de DI. Trimmed para a Fase 1: nao registra
/// handlers de comandos de negocio (status, armar, PGM, zonas) que nao existem
/// nesta base ainda — o unico handler de negocio (evento -> Ocorrencia) e
/// registrado pelo projeto Infrastructure, que tem acesso ao banco.
/// </summary>
public static class JflServiceCollectionExtensions
{
    public static IServiceCollection AddJflServer(this IServiceCollection services, Action<JflServerOptions>? configure = null)
    {
        var options = new JflServerOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddSingleton<SessionManager>();
        services.TryAddSingleton<ICentralAuthorizationProvider, LiberarTodasCentraisAuthorizationProvider>();

        services.AddSingleton<IJflCommandHandler, ConnectionCommandHandler>();
        services.AddSingleton<IJflCommandHandler, KeepAliveCommandHandler>();

        services.AddSingleton<JflCommandDispatcher>();
        services.AddSingleton<JflTcpServer>();

        return services;
    }
}
