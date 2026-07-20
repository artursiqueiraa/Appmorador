using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using AppMorador.Jfl.Protocol;

namespace AppMorador.Jfl.Server;

/// <summary>
/// Uma conexao TCP persistente com um equipamento JFL. Encapsula o stream (real ou,
/// em testes, um duplo de teste), o leitor de frames e o estado da sessao. Uma
/// instancia existe desde a aceitacao do socket ate o encerramento da conexao —
/// ela sobrevive a varios pacotes (conexao, keep-alives, eventos, comandos).
/// </summary>
public sealed class JflSession : IDisposable
{
    private readonly Stream _stream;
    private readonly IDisposable? _recursoSubjacente;
    private readonly SemaphoreSlim _travaEscrita = new(1, 1);
    private readonly ConcurrentDictionary<byte, TaskCompletionSource<JflPacket>> _requisicoesPendentes = new();
    private byte _proximoSeq;

    public Guid Id { get; } = Guid.NewGuid();

    public string RemoteEndPoint { get; }

    public string? RemoteIp { get; }

    public JflFrameReader Reader { get; }

    public JflSessionState State { get; set; } = JflSessionState.Conectando;

    /// <summary>Numero de serie do equipamento, preenchido apos o comando de conexao (0x21/0x2A).</summary>
    public string? NumeroSerie { get; set; }

    public string? Imei { get; set; }

    public string? Mac { get; set; }

    public byte? Modelo { get; set; }

    public string? VersaoFirmware { get; set; }

    public DateTimeOffset ConectadoEmUtc { get; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UltimaAtividadeUtc { get; private set; } = DateTimeOffset.UtcNow;

    public JflSession(Stream stream, string remoteEndPoint, IDisposable? recursoSubjacente = null, string? remoteIp = null)
    {
        _stream = stream;
        _recursoSubjacente = recursoSubjacente;
        RemoteEndPoint = remoteEndPoint;
        RemoteIp = remoteIp;
        Reader = new JflFrameReader(stream);
    }

    /// <summary>Cria uma sessao a partir de um <see cref="TcpClient"/> real aceito pelo listener.</summary>
    public static JflSession FromTcpClient(TcpClient client)
    {
        var stream = client.GetStream();
        var ipEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
        var remoteEndPoint = ipEndPoint?.ToString() ?? client.Client.RemoteEndPoint?.ToString() ?? "desconhecido";
        var remoteIp = ipEndPoint?.Address.ToString();
        return new JflSession(stream, remoteEndPoint, client, remoteIp);
    }

    public void MarcarAtividade() => UltimaAtividadeUtc = DateTimeOffset.UtcNow;

    public Task<JflPacket?> ReceiveAsync(CancellationToken cancellationToken) => Reader.ReadPacketAsync(cancellationToken);

    /// <summary>Gera o proximo byte de sequencia para um pacote iniciado pelo servidor (nunca 0x00).</summary>
    public byte NextSeq()
    {
        var seq = unchecked(++_proximoSeq);
        if (seq == 0)
        {
            seq = unchecked(++_proximoSeq);
        }

        return seq;
    }

    public async Task SendAsync(byte seq, byte cmd, ReadOnlyMemory<byte> dados, CancellationToken cancellationToken)
    {
        var pacote = PacketBuilder.Build(seq, cmd, dados.Span);

        await _travaEscrita.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(pacote, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _travaEscrita.Release();
        }
    }

    /// <summary>Responde a um pacote recebido, ecoando o mesmo SEQ (padrao de todo o protocolo JFL).</summary>
    public Task ReplyAsync(JflPacket requisicao, byte cmd, ReadOnlyMemory<byte> dados, CancellationToken cancellationToken) =>
        SendAsync(requisicao.Seq, cmd, dados, cancellationToken);

    /// <summary>
    /// Chamado pelo loop de leitura para cada pacote recebido, antes do dispatch
    /// normal por comando. Retorna <c>true</c> quando o pacote e a resposta de uma
    /// chamada pendente (nesse caso o chamador nao deve repassar o pacote a nenhum
    /// <c>IJflCommandHandler</c>). Nesta base (Fase 1) nao ha nenhum comando iniciado
    /// pelo servidor ainda, entao isto sempre retorna <c>false</c> — mantido para
    /// compatibilidade futura com comandos do tipo "servidor pergunta, central responde".
    /// </summary>
    public bool TryCompletePendingRequest(JflPacket packet)
    {
        if (_requisicoesPendentes.TryRemove(packet.Seq, out var tcs))
        {
            return tcs.TrySetResult(packet);
        }

        return false;
    }

    public void Close()
    {
        State = JflSessionState.Encerrada;

        foreach (var seq in _requisicoesPendentes.Keys.ToArray())
        {
            if (_requisicoesPendentes.TryRemove(seq, out var tcs))
            {
                tcs.TrySetException(new IOException($"Sessao encerrada antes da resposta ao SEQ 0x{seq:X2} chegar."));
            }
        }

        try
        {
            _stream.Dispose();
        }
        catch
        {
            // Encerramento best-effort: a conexao pode ja estar quebrada do lado remoto.
        }

        try
        {
            _recursoSubjacente?.Dispose();
        }
        catch
        {
            // idem.
        }
    }

    public void Dispose() => Close();
}
