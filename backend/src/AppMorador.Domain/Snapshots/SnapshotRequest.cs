namespace AppMorador.Domain.Snapshots;

/// <summary>
/// Concentra todos os parametros necessarios para um ISnapshotProvider capturar um
/// JPEG — evita espalhar Ip/Porta/Username/Password/Canal como parametros soltos de
/// metodo.
/// </summary>
public sealed class SnapshotRequest
{
    public required string Ip { get; init; }

    public required int Porta { get; init; }

    public required string Username { get; init; }

    public required string Password { get; init; }

    public required int Canal { get; init; }

    /// <summary>Propagado por toda a cadeia de captura (provider -> HTTP), em vez de ser um parametro solto de metodo.</summary>
    public required CancellationToken CancellationToken { get; init; }
}
