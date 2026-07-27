namespace AppMorador.Application.Cameras;

/// <summary>
/// Sprint 20 — payload do evento leve em tempo real "CameraStatusAlterado". Deliberadamente
/// separado do Snapshot Operacional (ADR 0016/0017): câmera é uma feature de exibição,
/// não faz parte do cálculo de saúde operacional da propriedade — inchar
/// <c>SnapshotOperacionalResponse</c> com isso acoplaria dois conceitos que não têm
/// relação nenhuma hoje.
/// </summary>
public sealed class CameraStatusEvento
{
    public required Guid CameraId { get; init; }

    public required string Status { get; init; }

    public string? UltimaImagemUrl { get; init; }

    public DateTime? UltimaAtualizacaoUtc { get; init; }
}
