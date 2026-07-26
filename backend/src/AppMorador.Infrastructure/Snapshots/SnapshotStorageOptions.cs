namespace AppMorador.Infrastructure.Snapshots;

public sealed class SnapshotStorageOptions
{
    /// <summary>Pasta raiz onde os snapshots sao gravados (relativa ao diretorio de trabalho do processo, ou absoluta).</summary>
    public string BasePath { get; set; } = "snapshots";

    /// <summary>Timeout (segundos) da chamada HTTP de captura. Configuravel via Snapshots:TimeoutSeconds.</summary>
    public int TimeoutSeconds { get; set; } = 5;
}
