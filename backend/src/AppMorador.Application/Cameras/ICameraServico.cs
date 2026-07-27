using AppMorador.Application.Common;

namespace AppMorador.Application.Cameras;

public interface ICameraServico
{
    Task<Result<IReadOnlyList<CameraResponse>>> ListByPropriedadeAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>Metadados da última captura já salva — nunca dispara uma nova. <c>Result.Ok(null)</c> quando a câmera existe mas nunca teve uma captura bem-sucedida (controller devolve 204).</summary>
    Task<Result<CameraSnapshotResponse?>> ObterSnapshotAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken);

    /// <summary>Dispara uma captura nova sob demanda (botão "Atualizar imagem") — timeout próprio de 15s, nunca lança.</summary>
    Task<Result<CameraSnapshotResponse>> CapturarSnapshotAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken);

    Task<Result<CameraStatusResponse>> ObterStatusAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken);

    /// <summary>Abre o stream da última imagem salva, para o Controller servir via <c>FileStreamResult</c>. Fail quando não há imagem/câmera/posse.</summary>
    Task<Result<CameraImagemArquivo>> ObterImagemAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken);
}
