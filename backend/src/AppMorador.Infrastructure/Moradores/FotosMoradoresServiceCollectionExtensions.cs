using AppMorador.Domain.Moradores;
using Microsoft.Extensions.DependencyInjection;

namespace AppMorador.Infrastructure.Moradores;

/// <summary>Akteck Seg (ADR 0036) — registra o armazenamento local das fotos de rosto dos moradores.</summary>
public static class FotosMoradoresServiceCollectionExtensions
{
    public static IServiceCollection AddFotosMoradores(this IServiceCollection services, Action<ArmazenamentoFotoMoradorOptions>? configure = null)
    {
        var options = new ArmazenamentoFotoMoradorOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<IArmazenamentoFotoMorador, ArmazenamentoFotoMorador>();
        return services;
    }
}
