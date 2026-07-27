namespace AppMorador.Domain.Snapshots;

/// <summary>
/// Sprint 20 — porta para o orquestrador de captura (implementação real em
/// Infrastructure, <c>SnapshotCaptureService</c>). Existe para que Application
/// (<c>CameraServico</c>) possa disparar uma captura sob demanda sem depender
/// diretamente de um tipo de Infrastructure — mesma regra de camadas já seguida por
/// todo Provider de fabricante (<c>IJflProvider</c>, <c>IControlIdProvider</c>...).
/// </summary>
public interface ISnapshotCaptureService
{
    /// <summary>Captura disparada por um alarme (Zona) — usada por <c>AlarmEventProcessor</c>.</summary>
    Task<SnapshotResult> CapturarAsync(Guid propriedadeId, Guid zonaId, DateTime recebidoEmUtc, CancellationToken cancellationToken);

    /// <summary>Captura sob demanda (botão "Atualizar imagem"), sem depender de uma Zona/alarme.</summary>
    Task<SnapshotResult> CapturarPorCameraIdAsync(Guid cameraId, DateTime recebidoEmUtc, CancellationToken cancellationToken);
}
