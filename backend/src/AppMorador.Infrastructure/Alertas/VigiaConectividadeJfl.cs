using AppMorador.Infrastructure.Saude;
using AppMorador.Application.Alertas;
using AppMorador.Application.Eventos;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using AppMorador.Jfl.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Alertas;

/// <summary>
/// Sprint 24 (ADR 0040) — detecta central JFL sem comunicação, inclusive a queda silenciosa
/// (conexão TCP aberta, mas sem keep-alive). A cada <see cref="Intervalo"/>, para cada Equipamento
/// JFL ativo:
/// - sem sessão no <see cref="SessionManager"/>, ou sem atividade há mais de 3× o keep-alive
///   combinado → Offline + evento "Perdida" + alerta crítico "Central sem comunicação";
/// - comunicando de novo depois de uma "Perdida" → Online + evento "Restabelecida" e o alerta para
///   de escalonar.
///
/// Por que um vigia periódico e não reagir direto a <see cref="SessionManager.SessaoRemovida"/>: a
/// reconexão de uma central dispara SessaoRemovida da sessão antiga um instante antes de registrar a
/// nova — reagir ao evento geraria alarme falso a cada reconexão. Olhando o estado a cada 30 s, a
/// reconexão rápida nunca é vista como queda. Também espera 3× o keep-alive depois de o servidor
/// subir, tempo para as centrais voltarem a discar.
/// </summary>
public sealed class VigiaConectividadeJfl : BackgroundService
{
    public static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);

    private readonly SessionManager _sessoes;
    private readonly JflServerOptions _opcoes;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VigiaConectividadeJfl> _logger;
    private readonly DateTimeOffset _iniciadoEmUtc = DateTimeOffset.UtcNow;

    public VigiaConectividadeJfl(
        SessionManager sessoes, JflServerOptions opcoes, IServiceScopeFactory scopeFactory, ILogger<VigiaConectividadeJfl> logger)
    {
        _sessoes = sessoes;
        _opcoes = opcoes;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>3× o keep-alive combinado com a central (1 a 20 min; fora disso a central assume 1).</summary>
    public TimeSpan LimiteSilencio => TimeSpan.FromMinutes(3 * (_opcoes.IntervaloKeepAliveMinutos is >= 1 and <= 20 ? _opcoes.IntervaloKeepAliveMinutos : 1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (DateTimeOffset.UtcNow - _iniciadoEmUtc < LimiteSilencio)
            {
                _scopeFactory.RegistrarBatimento("vigia-jfl", "Vigia de conexão das centrais", Intervalo);
                continue; // carência pós-início: as centrais ainda estão voltando a discar
            }

            try
            {
                await VerificarAsync(DateTimeOffset.UtcNow, stoppingToken).ConfigureAwait(false);
                _scopeFactory.RegistrarBatimento("vigia-jfl", "Vigia de conexão das centrais", Intervalo);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha na verificacao de conectividade das centrais JFL");
            }
        }
    }

    /// <summary>Uma rodada de verificação. Public para os testes chamarem sem esperar o timer.</summary>
    public async Task VerificarAsync(DateTimeOffset agoraUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var equipamentos = await db.Equipamentos
            .Where(e => e.Fabricante == FabricanteEquipamento.Jfl
                && e.Identificador != null
                && e.EstadoOperacional == EstadoOperacionalEquipamento.Ativo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var equipamento in equipamentos)
        {
            var comunicando = _sessoes.TryGet(equipamento.Identificador!, out var sessao)
                && sessao is not null
                && agoraUtc - sessao.UltimaAtividadeUtc <= LimiteSilencio;

            var ultimo = await db.EventosConectividade
                .Where(c => c.EquipamentoId == equipamento.Id)
                .OrderByDescending(c => c.OcorridoEmUtc)
                .Select(c => (TipoEventoConectividade?)c.Tipo)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!comunicando && equipamento.Status == StatusEquipamento.Online)
            {
                await RegistrarPerdaAsync(scope.ServiceProvider, db, equipamento, agoraUtc.UtcDateTime, cancellationToken).ConfigureAwait(false);
            }
            else if (comunicando && ultimo == TipoEventoConectividade.Perdida)
            {
                await RegistrarRetornoAsync(scope.ServiceProvider, db, equipamento, agoraUtc.UtcDateTime, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RegistrarPerdaAsync(
        IServiceProvider servicos, AppDbContext db, Equipamento equipamento, DateTime agoraUtc, CancellationToken cancellationToken)
    {
        equipamento.Status = StatusEquipamento.Offline;
        db.EventosConectividade.Add(new EventoConectividade
        {
            Id = Guid.NewGuid(),
            EquipamentoId = equipamento.Id,
            PropriedadeId = equipamento.PropriedadeId,
            Tipo = TipoEventoConectividade.Perdida,
            OcorridoEmUtc = agoraUtc,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "Central JFL {NumeroSerie} (Equipamento {EquipamentoId}) sem comunicacao ha mais de {Limite} — marcada Offline",
            equipamento.Identificador, equipamento.Id, LimiteSilencio);

        await PublicarAsync(servicos, equipamento.PropriedadeId, "Central sem comunicação", equipamento.Nome, destaque: true, agoraUtc, cancellationToken)
            .ConfigureAwait(false);

        await servicos.GetRequiredService<IAlertaCriticoServico>().CriarAsync(
            new NovoAlertaCritico
            {
                PropriedadeId = equipamento.PropriedadeId,
                Tipo = TipoAlertaCritico.SemComunicacao,
                Descricao = $"{equipamento.Nome} parou de se comunicar",
                EquipamentoId = equipamento.Id,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task RegistrarRetornoAsync(
        IServiceProvider servicos, AppDbContext db, Equipamento equipamento, DateTime agoraUtc, CancellationToken cancellationToken)
    {
        equipamento.Status = StatusEquipamento.Online;
        db.EventosConectividade.Add(new EventoConectividade
        {
            Id = Guid.NewGuid(),
            EquipamentoId = equipamento.Id,
            PropriedadeId = equipamento.PropriedadeId,
            Tipo = TipoEventoConectividade.Restabelecida,
            OcorridoEmUtc = agoraUtc,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Central JFL {NumeroSerie} (Equipamento {EquipamentoId}) voltou a se comunicar", equipamento.Identificador, equipamento.Id);

        await PublicarAsync(servicos, equipamento.PropriedadeId, "Comunicação restabelecida", equipamento.Nome, destaque: false, agoraUtc, cancellationToken)
            .ConfigureAwait(false);
        await servicos.GetRequiredService<IAlertaCriticoServico>()
            .EncerrarEscalonamentoAsync(equipamento.PropriedadeId, TipoAlertaCritico.SemComunicacao, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Tempo real (Central de Eventos + saúde do Dashboard). Melhor esforço.</summary>
    private async Task PublicarAsync(
        IServiceProvider servicos, Guid propriedadeId, string titulo, string nomeEquipamento, bool destaque, DateTime agoraUtc, CancellationToken cancellationToken)
    {
        try
        {
            var publicador = servicos.GetRequiredService<IOperacionalEventoPublicador>();
            await publicador.PublicarNovoEventoAsync(
                propriedadeId,
                new EventoResponse { Id = Guid.NewGuid(), Titulo = titulo, Descricao = nomeEquipamento, OcorridoEmUtc = agoraUtc, Destaque = destaque },
                cancellationToken).ConfigureAwait(false);

            await servicos.GetRequiredService<ISnapshotOperacionalServico>()
                .RegenerarEPublicarAsync(propriedadeId, MotivoAtualizacaoOperacional.EquipamentoStatusAlterado, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar mudanca de conectividade em tempo real (propriedade {PropriedadeId})", propriedadeId);
        }
    }
}
