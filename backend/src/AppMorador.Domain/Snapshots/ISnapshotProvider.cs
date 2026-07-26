using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Snapshots;

/// <summary>
/// Captura um JPEG instantaneo de uma camera. Uma implementacao por fabricante de
/// DVR (Dahua/Intelbras via CGI, Hikvision via ISAPI); a resolucao de qual
/// implementacao usar e feita por quem consome esta interface, por
/// <see cref="Fabricante"/>.
/// </summary>
public interface ISnapshotProvider
{
    FabricanteGravador Fabricante { get; }

    /// <summary>
    /// Retorna os bytes do JPEG, ou null se a captura falhar (timeout, HTTP nao-2xx,
    /// etc.). O cancelamento vem de <see cref="SnapshotRequest.CancellationToken"/>,
    /// nao de um parametro separado — concentra tudo no request.
    /// </summary>
    Task<byte[]?> CaptureAsync(SnapshotRequest request);
}
