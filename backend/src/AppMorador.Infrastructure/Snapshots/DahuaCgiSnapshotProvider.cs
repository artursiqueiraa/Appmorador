using AppMorador.Domain.Entities;

namespace AppMorador.Infrastructure.Snapshots;

internal sealed class DahuaCgiSnapshotProvider : CgiSnapshotProviderBase
{
    public DahuaCgiSnapshotProvider(IHttpClientFactory httpClientFactory, SnapshotStorageOptions options)
        : base(httpClientFactory, options)
    {
    }

    public override FabricanteGravador Fabricante => FabricanteGravador.Dahua;
}
