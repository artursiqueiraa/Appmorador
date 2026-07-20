using AppMorador.Application.Autenticacao;
using AppMorador.Application.Dashboard;
using AppMorador.Application.Eventos;
using AppMorador.Application.Propriedades;
using AppMorador.Domain.Repositories;
using AppMorador.Infrastructure.Dashboard;
using AppMorador.Infrastructure.Eventos;
using AppMorador.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AppMorador.Infrastructure.Identity;

/// <summary>Registra Autenticacao/Propriedade/Dashboard (Application + Infrastructure) no DI, mesmo padrao de AddJflServer/AddSnapshotCapture.</summary>
public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, Action<JwtOptions> configure)
    {
        var options = new JwtOptions { Key = string.Empty, Issuer = string.Empty, Audience = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key nao configurada. Configure via user-secrets em dev (dotnet user-secrets set \"Jwt:Key\" \"...\") " +
                "ou variavel de ambiente Jwt__Key em producao — nunca no appsettings.json.");
        }

        services.AddSingleton(options);

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IPropriedadeRepositorio, PropriedadeRepositorio>();
        services.AddScoped<IConsultaDashboardServico, ConsultaDashboardServico>();
        services.AddScoped<IFonteEventos, JflFonteEventos>();

        services.AddScoped<IAutenticacaoServico, AutenticacaoServico>();
        services.AddScoped<IPropriedadeServico, PropriedadeServico>();
        services.AddScoped<IDashboardServico, DashboardServico>();
        services.AddScoped<IEventosServico, EventosServico>();

        return services;
    }
}
