using AppMorador.Domain.Snapshots;
using Microsoft.Extensions.DependencyInjection;

namespace AppMorador.Infrastructure.Snapshots;

/// <summary>Registra os providers de snapshot, o storage local e o servico orquestrador no DI.</summary>
public static class SnapshotServiceCollectionExtensions
{
    public static IServiceCollection AddSnapshotCapture(this IServiceCollection services, Action<SnapshotStorageOptions>? configure = null)
    {
        var options = new SnapshotStorageOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Client nomeado, sem BaseAddress/Credentials fixados no registro — cada
        // provider configura BaseAddress/Timeout na instancia obtida por chamada
        // (seguro: CreateClient() devolve um HttpClient novo a cada chamada, mesmo
        // reaproveitando o handler interno do pool). Nunca "new HttpClient()".
        services.AddHttpClient(SnapshotHttpClientNames.Default);

        services.AddSingleton<ISnapshotStorage, SnapshotStorage>();
        services.AddSingleton<ISnapshotProvider, DahuaCgiSnapshotProvider>();
        services.AddSingleton<ISnapshotProvider, IntelbrasCgiSnapshotProvider>();
        services.AddSingleton<ISnapshotProvider, HikvisionIsapiSnapshotProvider>();
        services.AddScoped<ICameraResolver, CameraResolver>();
        services.AddScoped<ISnapshotCaptureService, SnapshotCaptureService>();

        return services;
    }
}
