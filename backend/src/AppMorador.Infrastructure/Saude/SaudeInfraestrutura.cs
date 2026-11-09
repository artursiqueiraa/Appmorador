using AppMorador.Application.Email;
using AppMorador.Application.Saude;
using AppMorador.Domain.Repositories;
using AppMorador.Infrastructure.Persistence;
using AppMorador.Jfl.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Saude;

public static class BatimentoExtensions
{
    /// <summary>Registra o batimento de uma rotina (opcional: em testes o serviço pode não existir).</summary>
    public static void RegistrarBatimento(this IServiceScopeFactory scopeFactory, string chave, string nome, TimeSpan intervalo)
    {
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetService<IBatimentosServicos>()?.Registrar(chave, nome, intervalo);
    }
}

internal sealed class VerificadorBanco : IVerificadorBanco
{
    private readonly AppDbContext _db;

    public VerificadorBanco(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ConectaAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }
}

/// <summary>
/// Sem o servidor JFL registrado (testes, ferramentas), informa "não escutando" — nunca derruba a saúde
/// do resto.
/// </summary>
internal sealed class EstadoRecepcaoJfl : IEstadoRecepcaoJfl
{
    private readonly JflTcpServer? _servidor;
    private readonly SessionManager? _sessoes;

    public EstadoRecepcaoJfl(IServiceProvider servicos)
    {
        _servidor = servicos.GetService<JflTcpServer>();
        _sessoes = servicos.GetService<SessionManager>();
    }

    public bool Escutando => _servidor?.EstaEmExecucao ?? false;

    public int CentraisConectadas => _sessoes?.QuantidadeAtiva ?? 0;
}

/// <summary>
/// Sprint 27 (ADR 0043) — a cada minuto verifica a saúde; componente fora há mais de 2 minutos gera
/// e-mail aos usuários Master (Resend; sem chave, fica no log) e o painel mostra o destaque. Quando
/// volta, avisa de novo. Queda do próprio backend não tem como ser avisada daqui: fica para um
/// monitor externo chamando /health (ADR 0043, pendência).
/// </summary>
public sealed class VigiaSaudeHostedService : BackgroundService
{
    public static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitorQuedas _monitor;
    private readonly ILogger<VigiaSaudeHostedService> _logger;

    public VigiaSaudeHostedService(IServiceScopeFactory scopeFactory, MonitorQuedas monitor, ILogger<VigiaSaudeHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _monitor = monitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await ExecutarRodadaAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha no vigia de saúde");
            }
        }
    }

    public async Task ExecutarRodadaAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var saude = await scope.ServiceProvider.GetRequiredService<ISaudeSistemaServico>().VerificarAsync(cancellationToken).ConfigureAwait(false);
        var (cairam, voltaram) = _monitor.Atualizar(saude.Componentes, DateTime.UtcNow);
        if (cairam.Count == 0 && voltaram.Count == 0)
        {
            return;
        }

        foreach (var c in cairam)
        {
            _logger.LogWarning("Saúde: {Componente} com problema há mais de 2 minutos — {Detalhe}", c.Nome, c.Detalhe);
        }

        foreach (var c in voltaram)
        {
            _logger.LogInformation("Saúde: {Componente} voltou ao normal", c.Nome);
        }

        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepositorio>();
        var email = scope.ServiceProvider.GetRequiredService<IEnvioEmail>();
        var masters = (await usuarios.ListInternosAsync(cancellationToken).ConfigureAwait(false))
            .Where(u => u.Ativo && u.RoleGlobal == Domain.Entities.RoleSistema.Master)
            .ToList();

        var assunto = cairam.Count > 0
            ? $"Akteck Seg: {string.Join(", ", cairam.Select(c => c.Nome))} com problema"
            : $"Akteck Seg: {string.Join(", ", voltaram.Select(c => c.Nome))} voltou ao normal";
        var linhas = cairam.Select(c => $"<li><b>{c.Nome}</b>: {c.Detalhe ?? "fora do ar"}</li>")
            .Concat(voltaram.Select(c => $"<li><b>{c.Nome}</b>: voltou ao normal</li>"));
        var html = $"<p>O vigia de saúde do sistema registrou:</p><ul>{string.Join(string.Empty, linhas)}</ul>"
            + "<p>Detalhes no painel admin, em Saúde do sistema.</p>";

        foreach (var master in masters)
        {
            await email.EnviarAsync(master.Email, assunto, html, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>Sprint 27 (ADR 0043) — composition root de segurança da conta e confiabilidade.</summary>
public static class SegurancaContaServiceCollectionExtensions
{
    public static IServiceCollection AddSegurancaContaModule(this IServiceCollection services)
    {
        services.AddScoped<IComandoExecutadoRepositorio, ComandoExecutadoRepositorio>();
        services.AddScoped<IRegistroBackupRepositorio, RegistroBackupRepositorio>();
        services.AddScoped<Application.Comandos.IPinConfirmacaoServico, Application.Comandos.PinConfirmacaoServico>();
        services.AddScoped<Application.Comandos.IExecucaoComandoServico, Application.Comandos.ExecucaoComandoServico>();
        services.AddScoped<Application.Comandos.IAuditoriaComandosServico, Application.Comandos.AuditoriaComandosServico>();

        services.AddSingleton<IBatimentosServicos, BatimentosServicos>();
        services.AddSingleton<MonitorQuedas>();
        services.AddScoped<IVerificadorBanco, VerificadorBanco>();
        services.AddSingleton<IEstadoRecepcaoJfl, EstadoRecepcaoJfl>();
        services.AddScoped<ISaudeSistemaServico, SaudeSistemaServico>();
        services.AddHostedService<VigiaSaudeHostedService>();
        return services;
    }
}
