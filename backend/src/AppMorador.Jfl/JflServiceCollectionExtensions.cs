using AppMorador.Jfl.Server;
using AppMorador.Jfl.Server.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppMorador.Jfl;

/// <summary>
/// Registra a infraestrutura de protocolo do servidor JFL (sessao, dispatcher,
/// handshake, keep-alive) no container de DI. O unico handler de negocio ligado a
/// um comando recebido (evento -> Ocorrencia) e registrado pelo projeto
/// Infrastructure, que tem acesso ao banco. Sprint 12 — Migracao JFL Active 100 Bus:
/// os servicos de comando iniciados pelo servidor (status/armar/PGM/inibir zonas)
/// sao registrados aqui — nao sao <see cref="IJflCommandHandler"/> (nao respondem a
/// um comando recebido), sao consumidos pelo Provider em Infrastructure/Jfl (ADR 0014).
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

        services.AddSingleton<CentralStatusQueryService>();
        services.AddSingleton<ArmCommandService>();
        services.AddSingleton<PgmCommandService>();
        services.AddSingleton<ZoneInhibitCommandService>();

        return services;
    }
}
