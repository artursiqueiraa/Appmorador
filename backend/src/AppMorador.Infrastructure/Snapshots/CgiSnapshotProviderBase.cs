using AppMorador.Domain.Entities;
using AppMorador.Domain.Snapshots;

namespace AppMorador.Infrastructure.Snapshots;

/// <summary>
/// Base compartilhada para os fabricantes que falam o mesmo endpoint CGI de
/// snapshot (Dahua e Intelbras, confirmado compativel na Fase 0:
/// `cgi-bin/snapshot.cgi?channel=N`, mesmo padrao ja usado em producao pelo
/// Teste-portaria-main1 para foto facial). Autenticacao Digest/Basic calculada por
/// requisicao em <see cref="DigestAuthHttpSender"/> (ver o motivo la).
/// </summary>
internal abstract class CgiSnapshotProviderBase : ISnapshotProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SnapshotStorageOptions _options;

    protected CgiSnapshotProviderBase(IHttpClientFactory httpClientFactory, SnapshotStorageOptions options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public abstract FabricanteGravador Fabricante { get; }

    public Task<byte[]?> CaptureAsync(SnapshotRequest request)
    {
        var client = _httpClientFactory.CreateClient(SnapshotHttpClientNames.Default);
        client.BaseAddress = new Uri($"http://{request.Ip}:{request.Porta}/");
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        return DigestAuthHttpSender.GetBytesAsync(
            client, $"cgi-bin/snapshot.cgi?channel={request.Canal}", request.Username, request.Password, request.CancellationToken);
    }
}
