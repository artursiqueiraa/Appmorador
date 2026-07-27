using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Snapshots;

/// <summary>
/// Responsavel apenas pela resolucao Zona -> VinculoZonaCamera -> Camera -> Gravador (canal
/// incluso, via Camera.Canal). Nao sabe nada sobre captura de snapshot nem storage —
/// isso e responsabilidade de <see cref="ISnapshotProvider"/>/<see cref="ISnapshotStorage"/>,
/// orquestrados por quem consome esta interface.
/// </summary>
public interface ICameraResolver
{
    /// <summary>Retorna a Camera (com Gravador carregado) vinculada a zona, ou null se nao houver vinculo.</summary>
    Task<Camera?> ResolveAsync(Guid zonaId, CancellationToken cancellationToken);

    /// <summary>Sprint 20 — resolve a Camera (com Gravador carregado) diretamente pelo proprio Id, para a captura sob demanda (sem depender de uma Zona/alarme).</summary>
    Task<Camera?> ResolveByIdAsync(Guid cameraId, CancellationToken cancellationToken);
}
