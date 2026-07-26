namespace AppMorador.Domain.Snapshots;

/// <summary>Resultado da captura+gravacao de um snapshot. Contem apenas o essencial.</summary>
public sealed class SnapshotResult
{
    public required bool Success { get; init; }

    public string? ImagePath { get; init; }

    public string? Error { get; init; }

    public static SnapshotResult Ok(string imagePath) => new() { Success = true, ImagePath = imagePath };

    public static SnapshotResult Fail(string error) => new() { Success = false, Error = error };
}
