using System.Text.Json;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using AppMorador.Jfl.Protocol;
using AppMorador.Jfl.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Jfl;

/// <summary>
/// Sprint 22C.2 — hook de conexão JFL: quando uma central termina o handshake
/// (<see cref="SessionManager.SessaoRegistrada"/>), procura o Equipamento correspondente
/// pelo Número de Série e marca automaticamente Online + descoberta (Modelo/MAC/Firmware,
/// só o que o handshake realmente devolveu — nunca abrindo conexão TCP de saída, a central
/// é sempre quem disca, ver ADR 0015). Não altera `AppMorador.Jfl` — só se inscreve num
/// evento que já existia. Mesmo padrão Singleton-com-scope-por-evento de
/// `EventoCommandHandler` (SessionManager/o evento são Singleton, o repositório é Scoped).
/// </summary>
public sealed class EquipamentoJflConexaoObserver : IHostedService
{
    private readonly SessionManager _sessionManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EquipamentoJflConexaoObserver> _logger;

    public EquipamentoJflConexaoObserver(
        SessionManager sessionManager, IServiceScopeFactory scopeFactory, ILogger<EquipamentoJflConexaoObserver> logger)
    {
        _sessionManager = sessionManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.SessaoRegistrada += OnSessaoRegistrada;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.SessaoRegistrada -= OnSessaoRegistrada;
        return Task.CompletedTask;
    }

    /// <summary>`SessaoRegistrada` é um evento síncrono — o trabalho real (I/O de banco) roda em segundo plano, nunca bloqueando o handshake JFL.</summary>
    private void OnSessaoRegistrada(JflSession session) => _ = ProcessarAsync(session);

    /// <summary>Internal (em vez de private) só para ser exercitado diretamente pelos testes, sem depender da corrida do fire-and-forget do evento.</summary>
    internal async Task ProcessarAsync(JflSession session)
    {
        if (string.IsNullOrEmpty(session.NumeroSerie))
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var equipamentos = scope.ServiceProvider.GetRequiredService<IEquipamentoRepositorio>();

            var equipamento = await equipamentos
                .GetByFabricanteEIdentificadorAsync(FabricanteEquipamento.Jfl, session.NumeroSerie, CancellationToken.None)
                .ConfigureAwait(false);

            if (equipamento is null)
            {
                _logger.LogInformation(
                    "Central JFL {NumeroSerie} conectou mas nenhum Equipamento cadastrado corresponde a este Número de Série",
                    session.NumeroSerie);
                return;
            }

            equipamento.Status = StatusEquipamento.Online;
            equipamento.UltimaSincronizacaoUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(session.Mac))
            {
                equipamento.MacAddress = session.Mac;
            }

            var descobertas = new Dictionary<string, string>();
            if (session.Modelo is not null)
            {
                descobertas["Modelo"] = session.Modelo.Value.ToNomeAmigavel();
            }

            if (!string.IsNullOrWhiteSpace(session.VersaoFirmware))
            {
                descobertas["Firmware"] = session.VersaoFirmware;
            }

            if (!string.IsNullOrWhiteSpace(session.Imei))
            {
                descobertas["Imei"] = session.Imei;
            }

            if (descobertas.Count > 0)
            {
                equipamento.InformacoesDescobertasJson = JsonSerializer.Serialize(descobertas);
                equipamento.UltimaDescobertaUtc = DateTime.UtcNow;
            }

            await equipamentos.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation(
                "Equipamento {EquipamentoId} (JFL, Número de Série {NumeroSerie}) marcado Online após conexão da central",
                equipamento.Id, session.NumeroSerie);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao processar conexão da central JFL {NumeroSerie} para atualização automática do Equipamento",
                session.NumeroSerie);
        }
    }
}
