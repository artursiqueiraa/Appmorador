namespace AppMorador.Domain.Snapshots;

/// <summary>Persiste os bytes de um snapshot em disco local e devolve o caminho relativo salvo.</summary>
public interface ISnapshotStorage
{
    Task<string> SaveAsync(Guid propriedadeId, DateTime capturedAtUtc, byte[] content, CancellationToken cancellationToken);

    /// <summary>
    /// Sprint 20 — abre o arquivo de um caminho relativo já salvo (mesmo formato de
    /// <see cref="SaveAsync"/>) para servir via Api (<c>GET /api/cameras/{id}/imagem</c>).
    /// Retorna null se o caminho não existir no disco (nunca lança) — quem chama decide
    /// como responder (404).
    /// </summary>
    Stream? OpenRead(string relativePath);
}
