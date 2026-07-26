using AppMorador.Domain.Snapshots;

namespace AppMorador.Infrastructure.Snapshots;

/// <summary>
/// Disco local, sem nuvem. Salva em {BasePath}/{propriedadeId}/{yyyy}/{MM}/{dd}/{guid}.jpg,
/// criando os diretorios automaticamente. Devolve o caminho relativo (com "/", nao
/// Path.DirectorySeparatorChar) para ser guardado em Ocorrencia.ImagePath
/// independente de sistema operacional.
/// </summary>
internal sealed class SnapshotStorage : ISnapshotStorage
{
    private readonly SnapshotStorageOptions _options;

    public SnapshotStorage(SnapshotStorageOptions options)
    {
        _options = options;
    }

    public async Task<string> SaveAsync(Guid propriedadeId, DateTime capturedAtUtc, byte[] content, CancellationToken cancellationToken)
    {
        var relativeDir = $"{_options.BasePath}/{propriedadeId:D}/{capturedAtUtc:yyyy}/{capturedAtUtc:MM}/{capturedAtUtc:dd}";
        var absoluteDir = Path.Combine(
            _options.BasePath, propriedadeId.ToString(), capturedAtUtc.ToString("yyyy"), capturedAtUtc.ToString("MM"), capturedAtUtc.ToString("dd"));

        Directory.CreateDirectory(absoluteDir);

        var fileName = $"{Guid.NewGuid():D}.jpg";
        var absolutePath = Path.Combine(absoluteDir, fileName);

        await File.WriteAllBytesAsync(absolutePath, content, cancellationToken).ConfigureAwait(false);

        return $"{relativeDir}/{fileName}";
    }
}
