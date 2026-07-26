using AppMorador.Domain.Entities;
using AppMorador.Domain.Snapshots;

namespace AppMorador.Infrastructure.Snapshots;

/// <summary>
/// Endpoint ISAPI padrao de snapshot: `/ISAPI/Streaming/channels/{id}/picture`, onde
/// {id} segue a convencao Hikvision de track principal (canal N -> "N01").
/// Autenticacao Digest/Basic calculada por requisicao em
/// <see cref="DigestAuthHttpSender"/> (ver o motivo la).
/// </summary>
internal sealed class HikvisionIsapiSnapshotProvider : ISnapshotProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SnapshotStorageOptions _options;

    public HikvisionIsapiSnapshotProvider(IHttpClientFactory httpClientFactory, SnapshotStorageOptions options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public FabricanteGravador Fabricante => FabricanteGravador.Hikvision;

    public Task<byte[]?> CaptureAsync(SnapshotRequest request)
    {
        var client = _httpClientFactory.CreateClient(SnapshotHttpClientNames.Default);
        client.BaseAddress = new Uri($"http://{request.Ip}:{request.Porta}/");
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        var canalIsapi = $"{request.Canal}01";

        return DigestAuthHttpSender.GetBytesAsync(
            client, $"ISAPI/Streaming/channels/{canalIsapi}/picture", request.Username, request.Password, request.CancellationToken);
    }
}
