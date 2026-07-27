using AppMorador.Application.Notificacoes;
using AppMorador.Domain.Repositories;
using AppMorador.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppMorador.Infrastructure.Notifications;

/// <summary>Sprint 19 (ADR 0023) — composition root do módulo de notificações push.</summary>
public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FirebaseOptions>(configuration.GetSection("Firebase"));

        services.AddScoped<IDispositivoPushRepositorio, DispositivoPushRepositorio>();
        services.AddScoped<IDispositivoPushServico, DispositivoPushServico>();
        services.AddScoped<INotificationProvider, FirebaseNotificationProvider>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // Singleton: o debounce so funciona se sobreviver entre requests (Fase 8.2).
        services.AddSingleton<IDebounceNotificacao, DebounceNotificacaoEmMemoria>();

        return services;
    }
}
