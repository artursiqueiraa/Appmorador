namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 20 — sem monitoramento contínuo (nenhum polling ativo de câmera existe hoje),
/// o status só reflete o resultado da ÚLTIMA tentativa de captura (via
/// <see cref="AppMorador.Domain.Snapshots.ISnapshotProvider"/>): sucesso vira Online,
/// falha vira Offline. <see cref="Desconhecido"/> é o valor inicial de uma câmera em
/// que nunca houve nenhuma tentativa (nem alarme, nem captura sob demanda).
/// </summary>
public enum StatusCamera
{
    Desconhecido,
    Online,
    Offline,
}
