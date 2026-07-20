using AppMorador.Jfl.Messages;
using AppMorador.Jfl.Protocol;
using AppMorador.Jfl.Server;
using AppMorador.Jfl.Server.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Jfl;

/// <summary>
/// Adaptador fino de protocolo para o comando de evento (0x24): so faz parse do
/// payload, responde o ACK imediatamente (antes de qualquer I/O), e delega todo o
/// processamento de negocio para <see cref="AlarmEventProcessor"/> (Scoped, por isso
/// resolvido via <see cref="IServiceScopeFactory"/> a partir deste handler Singleton).
/// Nao contem nenhuma regra de negocio propria — filtro, dedup, resolucao de
/// painel/zona e criacao de Ocorrencia vivem todos no processor.
/// </summary>
public sealed class EventoCommandHandler : IJflCommandHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventoCommandHandler> _logger;

    public EventoCommandHandler(IServiceScopeFactory scopeFactory, ILogger<EventoCommandHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public bool CanHandle(byte cmd) => cmd == (byte)JflCommand.Evento;

    public async Task HandleAsync(JflSession session, JflPacket packet, CancellationToken cancellationToken)
    {
        EventoRequest evento;
        try
        {
            evento = EventoRequest.Parse(packet.Dados);
        }
        catch (JflProtocolException ex)
        {
            _logger.LogError(
                ex,
                "Evento 0x24 malformado recebido de {RemoteEndPoint} (central {NumeroSerie}) — descartado sem ACK",
                session.RemoteEndPoint, session.NumeroSerie ?? "desconhecida");
            return;
        }

        // ACK primeiro — nunca espera banco, filtro, ou qualquer I/O.
        await session.ReplyAsync(packet, packet.Cmd, EventoResponse.BuildAck(evento), cancellationToken)
            .ConfigureAwait(false);

        var recebidoEmUtc = DateTime.UtcNow;

        _logger.LogInformation(
            "Evento JFL recebido: central {NumeroSerie} conta={Conta} codigo={Codigo} particao={Particao} " +
            "zonaOuUsuario={ZonaOuUsuario} contador=0x{Contador:X8}",
            session.NumeroSerie ?? "desconhecida", evento.Conta, evento.CodigoEvento, evento.Particao,
            evento.UsuarioOuZona, evento.Contador);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<AlarmEventProcessor>();
            await processor.ProcessarAsync(session.NumeroSerie, packet.Dados, evento, recebidoEmUtc, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao processar evento (central {NumeroSerie}, contador=0x{Contador:X8}) — " +
                "o evento ja foi confirmado (ACK) ao painel",
                session.NumeroSerie ?? "desconhecida", evento.Contador);
        }
    }
}
