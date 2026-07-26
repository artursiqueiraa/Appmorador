using AppMorador.Domain.Entities;

namespace AppMorador.Infrastructure.Snapshots;

/// <summary>
/// Intelbras usa o mesmo endpoint CGI do Dahua (confirmado na Fase 0 —
/// IntelbrasProvider.CaptureFaceFromCameraAsync do Teste-portaria-main1 ja usa
/// exatamente `cgi-bin/snapshot.cgi?channel=1` contra hardware Intelbras real).
/// </summary>
internal sealed class IntelbrasCgiSnapshotProvider : CgiSnapshotProviderBase
{
    public IntelbrasCgiSnapshotProvider(IHttpClientFactory httpClientFactory, SnapshotStorageOptions options)
        : base(httpClientFactory, options)
    {
    }

    public override FabricanteGravador Fabricante => FabricanteGravador.Intelbras;
}
