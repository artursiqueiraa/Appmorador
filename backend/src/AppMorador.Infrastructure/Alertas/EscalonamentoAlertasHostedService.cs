using AppMorador.Infrastructure.Saude;
using AppMorador.Application.Alertas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Alertas;

/// <summary>
/// Sprint 24 (ADR 0040) — a cada 30 s pede ao <see cref="IAlertaCriticoServico"/> para avançar os
/// alertas sem confirmação cujo prazo venceu. Só agenda; toda regra de quem é avisado vive no
/// serviço. Sem fila nem worker externo (regra de simplicidade dos MVPs): um timer basta enquanto o
/// backend roda numa instância — e o avanço condicional no banco já protege contra duas.
/// </summary>
public sealed class EscalonamentoAlertasHostedService : BackgroundService
{
    public static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EscalonamentoAlertasHostedService> _logger;

    public EscalonamentoAlertasHostedService(IServiceScopeFactory scopeFactory, ILogger<EscalonamentoAlertasHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var servico = scope.ServiceProvider.GetRequiredService<IAlertaCriticoServico>();
                var avancados = await servico.EscalonarVencidosAsync(DateTime.UtcNow, stoppingToken).ConfigureAwait(false);
                _scopeFactory.RegistrarBatimento("escalonamento", "Escalonamento de alertas", Intervalo);
                if (avancados > 0)
                {
                    _logger.LogInformation("{Quantidade} alerta(s) critico(s) escalonado(s)", avancados);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha no ciclo de escalonamento de alertas criticos");
            }
        }
    }
}
