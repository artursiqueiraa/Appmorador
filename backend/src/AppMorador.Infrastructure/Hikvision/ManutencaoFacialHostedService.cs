using AppMorador.Infrastructure.Saude;
using AppMorador.Application.Moradores;
using AppMorador.Domain.Moradores;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Hikvision;

/// <summary>
/// Sprint 26 (ADR 0042, LGPD) — a cada minuto:
/// 1. pessoa excluída que ainda tem rosto (ex.: excluída junto com a unidade/propriedade) → apaga a
///    foto do servidor e dos leitores;
/// 2. tenta de novo envios/remoções que falharam (leitor fora do ar);
/// 3. pessoa excluída há mais de <see cref="Retencao"/> → anonimiza nome e contatos (o histórico de
///    entradas continua, sem identificar a pessoa), conforme a Política de Privacidade.
/// </summary>
public sealed class ManutencaoFacialHostedService : BackgroundService
{
    public static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Retencao = TimeSpan.FromDays(183);
    public const string NomeAnonimizado = "Pessoa removida";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ManutencaoFacialHostedService> _logger;

    public ManutencaoFacialHostedService(IServiceScopeFactory scopeFactory, ILogger<ManutencaoFacialHostedService> logger)
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
                await ExecutarRodadaAsync(stoppingToken).ConfigureAwait(false);
                _scopeFactory.RegistrarBatimento("manutencao-facial", "Leitores faciais e retenção LGPD", Intervalo);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha na manutenção de dados faciais");
            }
        }
    }

    /// <summary>Uma rodada. Public para testes.</summary>
    public async Task ExecutarRodadaAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var armazenamento = scope.ServiceProvider.GetRequiredService<IArmazenamentoFotoMorador>();
        var leitores = scope.ServiceProvider.GetRequiredService<IFacialLeitoresServico>();

        var comFoto = await db.Moradores.IgnoreQueryFilters()
            .Include(m => m.Unidade)
            .Where(m => m.Excluido && m.FotoPath != null)
            .Take(50)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var morador in comFoto)
        {
            armazenamento.Remover(morador.FotoPath!);
            morador.FotoPath = null;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await leitores.SincronizarAsync(morador, null, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Rosto da pessoa excluída {MoradorId} apagado do servidor e dos leitores", morador.Id);
        }

        await leitores.ReprocessarFalhasAsync(cancellationToken).ConfigureAwait(false);

        var limite = DateTime.UtcNow - Retencao;
        var vencidos = await db.Moradores.IgnoreQueryFilters()
            .Where(m => m.Excluido && m.DataExclusaoUtc != null && m.DataExclusaoUtc < limite && m.Nome != NomeAnonimizado)
            .Take(200)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var morador in vencidos)
        {
            morador.Nome = NomeAnonimizado;
            morador.Documento = null;
            morador.Telefone = null;
            morador.Email = null;
            morador.Observacoes = null;
        }

        if (vencidos.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("{Quantidade} pessoa(s) excluída(s) há mais de 6 meses anonimizada(s)", vencidos.Count);
        }
    }
}
