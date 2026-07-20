using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using AppMorador.Jfl.Server.Handlers;
using Microsoft.Extensions.Logging;

namespace AppMorador.Jfl.Server;

/// <summary>
/// Servidor TCP compativel com o modelo de comunicacao da JFL: a central e quem
/// disca para fora, este processo apenas escuta e aceita. Cada conexao aceita vira
/// uma <see cref="JflSession"/> de longa duracao; pacotes recebidos sao roteados
/// pelo <see cref="JflCommandDispatcher"/> ate um handler.
/// </summary>
public sealed class JflTcpServer : IAsyncDisposable
{
    private readonly JflServerOptions _options;
    private readonly JflCommandDispatcher _dispatcher;
    private readonly SessionManager _sessionManager;
    private readonly ILogger<JflTcpServer> _logger;
    private readonly ConcurrentDictionary<Guid, Task> _handlersAtivos = new();

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loopDeAceitacao;

    public JflTcpServer(
        JflServerOptions options,
        JflCommandDispatcher dispatcher,
        SessionManager sessionManager,
        ILogger<JflTcpServer> logger)
    {
        _options = options;
        _dispatcher = dispatcher;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>Porta efetivamente em uso apos <see cref="Start"/> (util quando <see cref="JflServerOptions.Porta"/> e 0).</summary>
    public int Port { get; private set; }

    public bool EstaEmExecucao => _listener is not null;

    public void Start()
    {
        if (_listener is not null)
        {
            return;
        }

        var listener = new TcpListener(IPAddress.Any, _options.Porta);

        try
        {
            listener.Start();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            _logger.LogError(
                ex,
                "Nao foi possivel iniciar o servidor JFL: a porta {Port} ja esta em uso. " +
                "Verifique se outra instancia do backend ja esta rodando (mesmo em segundo plano) " +
                "ou se outro processo ocupa essa porta, e finalize-o antes de tentar novamente.",
                _options.Porta);
            throw;
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Nao foi possivel iniciar o servidor JFL na porta {Port}: erro de socket.", _options.Porta);
            throw;
        }

        _listener = listener;
        _cts = new CancellationTokenSource();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

        _logger.LogInformation("Servidor JFL escutando na porta {Port}", Port);

        _loopDeAceitacao = AceitarConexoesAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        _listener?.Stop();

        try
        {
            if (_loopDeAceitacao is not null)
            {
                await _loopDeAceitacao.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // esperado ao cancelar o loop de aceitacao.
        }

        var pendentes = _handlersAtivos.Values.ToArray();
        await Task.WhenAll(pendentes).ConfigureAwait(false);

        _listener = null;
        _logger.LogInformation("Servidor JFL parado (porta {Port} liberada)", Port);
    }

    private async Task AceitarConexoesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Erro de socket ao aceitar conexao");
                continue;
            }

            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            _logger.LogInformation(
                "Conexao TCP aceita: IP remoto={RemoteIp} Porta remota={RemotePort}",
                remoteEndPoint?.Address, remoteEndPoint?.Port);

            var session = JflSession.FromTcpClient(client);
            var handlerTask = HandleClientAsync(session, cancellationToken);
            _handlersAtivos[session.Id] = handlerTask;
            _ = handlerTask.ContinueWith(
                completedTask => _handlersAtivos.TryRemove(session.Id, out var _),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private async Task HandleClientAsync(JflSession session, CancellationToken serverCancellationToken)
    {
        _logger.LogInformation("Nova conexao TCP recebida de {RemoteEndPoint}", session.RemoteEndPoint);

        try
        {
            while (!serverCancellationToken.IsCancellationRequested)
            {
                var pacote = await session.ReceiveAsync(serverCancellationToken).ConfigureAwait(false);
                if (pacote is null)
                {
                    _logger.LogInformation(
                        "Conexao encerrada pelo equipamento: central {NumeroSerie} ({RemoteEndPoint})",
                        session.NumeroSerie ?? "desconhecida", session.RemoteEndPoint);
                    break;
                }

                session.MarcarAtividade();

                _logger.LogDebug(
                    "Pacote recebido de {RemoteEndPoint}: {Pacote}",
                    session.RemoteEndPoint, pacote);

                if (session.TryCompletePendingRequest(pacote))
                {
                    continue;
                }

                try
                {
                    await _dispatcher.DispatchAsync(session, pacote, serverCancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex, "Erro ao processar comando 0x{Cmd:X2} da central {NumeroSerie}",
                        pacote.Cmd, session.NumeroSerie ?? "desconhecida");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // esperado durante o encerramento do servidor.
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Conexao com {RemoteEndPoint} (central {NumeroSerie}) perdida", session.RemoteEndPoint, session.NumeroSerie ?? "desconhecida");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado na sessao de {RemoteEndPoint}", session.RemoteEndPoint);
        }
        finally
        {
            _logger.LogInformation(
                "Conexao finalizada: {RemoteEndPoint} (central {NumeroSerie})",
                session.RemoteEndPoint, session.NumeroSerie ?? "desconhecida");

            _sessionManager.Remover(session);
            session.Close();
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
