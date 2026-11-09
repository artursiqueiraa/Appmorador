using AppMorador.Application.Alertas;
using AppMorador.Application.ContatosEmergencia;
using AppMorador.Application.Eventos;
using AppMorador.Application.Conta;
using AppMorador.Application.Email;
using AppMorador.Application.Painel.Simulador;
using AppMorador.Application.Pessoas;
using Microsoft.Extensions.Configuration;
using AppMorador.Application.Sms;
using AppMorador.Domain.Repositories;
using AppMorador.Infrastructure.Eventos;
using AppMorador.Infrastructure.Notifications;
using AppMorador.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AppMorador.Infrastructure.Alertas;

/// <summary>Sprint 24 (ADR 0040) — composition root dos alertas confiáveis.</summary>
public static class AlertasServiceCollectionExtensions
{
    /// <summary>
    /// Serviços, repositórios, fontes da Central de Eventos e o escalonamento. O vigia de
    /// conectividade JFL fica de fora: depende do servidor JFL (<see cref="AddVigiaConectividadeJfl"/>).
    /// </summary>
    public static IServiceCollection AddAlertasModule(this IServiceCollection services, IConfiguration? configuracao = null)
    {
        // Sprint 25 (ADR 0041) — pessoas com acesso, convites (e-mail via Resend) e minha conta.
        var email = new EmailOptions();
        configuracao?.GetSection("Email").Bind(email);
        services.AddSingleton(email);
        services.AddHttpClient("Resend");
        services.AddSingleton<IEnvioEmail, EnvioEmailResend>();
        services.AddScoped<IConvitePropriedadeRepositorio, ConvitePropriedadeRepositorio>();
        services.AddScoped<ISessoesUsuario, SessoesUsuario>();
        services.AddScoped<IPessoasPropriedadeServico, PessoasPropriedadeServico>();
        services.AddScoped<IMinhaContaServico, MinhaContaServico>();

        // Sprint 26 (ADR 0042) — facial ponta a ponta + LGPD.
        services.AddScoped<AppMorador.Domain.Repositories.IConsentimentoBiometricoRepositorio, ConsentimentoBiometricoRepositorio>();
        services.AddScoped<AppMorador.Domain.Repositories.ISincronizacaoFotoRepositorio, SincronizacaoFotoRepositorio>();
        services.AddScoped<AppMorador.Application.Hikvision.IHikvisionAcessoProvider, AppMorador.Infrastructure.Hikvision.HikvisionAcessoProvider>();
        services.AddScoped<AppMorador.Application.Moradores.IFacialLeitoresServico, AppMorador.Application.Moradores.FacialLeitoresServico>();
        services.AddScoped<AppMorador.Application.Moradores.IConsentimentoBiometricoServico, AppMorador.Application.Moradores.ConsentimentoBiometricoServico>();
        services.AddHostedService<AppMorador.Infrastructure.Hikvision.ManutencaoFacialHostedService>();

        services.AddScoped<IAlertaCriticoRepositorio, AlertaCriticoRepositorio>();
        services.AddScoped<IContatoEmergenciaRepositorio, ContatoEmergenciaRepositorio>();
        services.AddScoped<IAlertaCriticoServico, AlertaCriticoServico>();
        services.AddScoped<IContatoEmergenciaServico, ContatoEmergenciaServico>();
        services.AddScoped<ISimuladorEventosServico, SimuladorEventosServico>();

        // Decisão B — SMS simulado até existir conta num provedor.
        services.AddSingleton<IEnvioSms, EnvioSmsSimulado>();

        services.AddScoped<IFonteEventos, AlertasFonteEventos>();
        services.AddScoped<IFonteEventos, ConectividadeFonteEventos>();

        services.AddHostedService<EscalonamentoAlertasHostedService>();
        return services;
    }

    public static IServiceCollection AddVigiaConectividadeJfl(this IServiceCollection services)
    {
        services.AddHostedService<VigiaConectividadeJfl>();
        return services;
    }
}
