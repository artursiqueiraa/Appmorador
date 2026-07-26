namespace AppMorador.Domain.Snapshots;

/// <summary>Persiste os bytes de um snapshot em disco local e devolve o caminho relativo salvo.</summary>
public interface ISnapshotStorage
{
    Task<string> SaveAsync(Guid propriedadeId, DateTime capturedAtUtc, byte[] content, CancellationToken cancellationToken);
}
