using AppMorador.Application.Common;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using AppMorador.Domain.Snapshots;

namespace AppMorador.Application.Cameras;

/// <summary>
/// Ownership resolvido via Camera.Propriedade.ProprietarioId (mesmo padrão de todo o
/// domínio). Sprint 20 — sem monitoramento contínuo: <see cref="Camera.Status"/> só
/// muda no momento de uma tentativa de captura (aqui, sob demanda; ou no fluxo de
/// alarme, já existente). Nunca calcula/pinga a câmera em segundo plano.
/// </summary>
public sealed class CameraServico : ICameraServico
{
    // Sprint 20 (Fase 2.3 da missão) — limite superior do tempo que o morador espera
    // pelo botão "Atualizar imagem". Camada extra sobre o timeout já configurado em
    // Snapshots:TimeoutSeconds (que protege cada chamada HTTP ao gravador
    // individualmente) — este aqui garante que a REQUISIÇÃO do mobile nunca fica
    // pendurada além de 15s, mesmo que o valor configurado mude no futuro.
    private const int TimeoutSegundosCapturaSobDemanda = 15;

    private readonly IPropriedadeRepositorio _propriedades;
    private readonly ICameraRepositorio _cameras;
    private readonly ISnapshotStorage _storage;
    private readonly ISnapshotCaptureService _snapshotCaptureService;
    private readonly IOperacionalEventoPublicador _publicador;

    public CameraServico(
        IPropriedadeRepositorio propriedades,
        ICameraRepositorio cameras,
        ISnapshotStorage storage,
        ISnapshotCaptureService snapshotCaptureService,
        IOperacionalEventoPublicador publicador)
    {
        _propriedades = propriedades;
        _cameras = cameras;
        _storage = storage;
        _snapshotCaptureService = snapshotCaptureService;
        _publicador = publicador;
    }

    public async Task<Result<IReadOnlyList<CameraResponse>>> ListByPropriedadeAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<CameraResponse>>.Fail("Propriedade não encontrada.");
        }

        var cameras = await _cameras.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<CameraResponse>>.Ok(cameras.Select(ToResponse).ToList());
    }

    public async Task<Result<CameraSnapshotResponse?>> ObterSnapshotAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken)
    {
        var camera = await ResolverCameraAsync(proprietarioId, cameraId, cancellationToken).ConfigureAwait(false);
        if (camera is null)
        {
            return Result<CameraSnapshotResponse?>.Fail("Câmera não encontrada.");
        }

        if (camera.UltimoSnapshotPath is null)
        {
            return Result<CameraSnapshotResponse?>.Ok(null);
        }

        return Result<CameraSnapshotResponse?>.Ok(new CameraSnapshotResponse
        {
            Sucesso = true,
            UltimaImagemUrl = MontarUrlImagem(camera),
            CapturadaEmUtc = camera.UltimoSucessoCapturaUtc,
            Status = camera.Status,
        });
    }

    public async Task<Result<CameraSnapshotResponse>> CapturarSnapshotAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken)
    {
        var camera = await ResolverCameraAsync(proprietarioId, cameraId, cancellationToken).ConfigureAwait(false);
        if (camera is null)
        {
            return Result<CameraSnapshotResponse>.Fail("Câmera não encontrada.");
        }

        var agora = DateTime.UtcNow;
        camera.UltimaTentativaCapturaUtc = agora;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(TimeoutSegundosCapturaSobDemanda));

        SnapshotResult resultado;
        string? motivoAmigavel;
        try
        {
            resultado = await _snapshotCaptureService.CapturarPorCameraIdAsync(cameraId, agora, timeoutCts.Token).ConfigureAwait(false);
            motivoAmigavel = resultado.Success ? null : MapearMotivoAmigavel(resultado.Error);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            resultado = SnapshotResult.Fail("Tempo esgotado ao aguardar a câmera.");
            motivoAmigavel = "A câmera demorou demais para responder.";
        }

        var statusAnterior = camera.Status;
        if (resultado.Success)
        {
            camera.UltimoSnapshotPath = resultado.ImagePath;
            camera.UltimoSucessoCapturaUtc = agora;
            camera.Status = StatusCamera.Online;
        }
        else
        {
            camera.Status = StatusCamera.Offline;
        }

        await _cameras.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (statusAnterior != camera.Status)
        {
            await _publicador.PublicarCameraStatusAsync(
                camera.PropriedadeId,
                new CameraStatusEvento
                {
                    CameraId = camera.Id,
                    Status = camera.Status.ToString(),
                    UltimaImagemUrl = MontarUrlImagem(camera),
                    UltimaAtualizacaoUtc = camera.UltimoSucessoCapturaUtc,
                },
                cancellationToken).ConfigureAwait(false);
        }

        return Result<CameraSnapshotResponse>.Ok(new CameraSnapshotResponse
        {
            Sucesso = resultado.Success,
            MensagemErro = motivoAmigavel,
            UltimaImagemUrl = MontarUrlImagem(camera),
            CapturadaEmUtc = camera.UltimoSucessoCapturaUtc,
            Status = camera.Status,
        });
    }

    public async Task<Result<CameraStatusResponse>> ObterStatusAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken)
    {
        var camera = await ResolverCameraAsync(proprietarioId, cameraId, cancellationToken).ConfigureAwait(false);
        if (camera is null)
        {
            return Result<CameraStatusResponse>.Fail("Câmera não encontrada.");
        }

        return Result<CameraStatusResponse>.Ok(new CameraStatusResponse
        {
            Status = camera.Status,
            UltimaTentativaCapturaUtc = camera.UltimaTentativaCapturaUtc,
            UltimoSucessoCapturaUtc = camera.UltimoSucessoCapturaUtc,
            MotivoIndisponibilidade = camera.Status == StatusCamera.Offline
                ? "Não foi possível obter uma imagem desta câmera recentemente."
                : null,
        });
    }

    public async Task<Result<CameraImagemArquivo>> ObterImagemAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken)
    {
        var camera = await ResolverCameraAsync(proprietarioId, cameraId, cancellationToken).ConfigureAwait(false);
        if (camera?.UltimoSnapshotPath is null)
        {
            return Result<CameraImagemArquivo>.Fail("Imagem não encontrada.");
        }

        var stream = _storage.OpenRead(camera.UltimoSnapshotPath);
        if (stream is null)
        {
            return Result<CameraImagemArquivo>.Fail("Imagem não encontrada.");
        }

        return Result<CameraImagemArquivo>.Ok(new CameraImagemArquivo { Conteudo = stream, ContentType = SniffarContentType(stream) });
    }

    /// <summary>Sniffa a assinatura do arquivo em vez de assumir JPEG sempre — ver <see cref="CameraImagemArquivo"/>.</summary>
    private static string SniffarContentType(Stream stream)
    {
        Span<byte> assinatura = stackalloc byte[8];
        var lidos = stream.Read(assinatura);
        stream.Seek(0, SeekOrigin.Begin);

        var ehPng = lidos >= 8 && assinatura[0] == 0x89 && assinatura[1] == 0x50 && assinatura[2] == 0x4E && assinatura[3] == 0x47;
        return ehPng ? "image/png" : "image/jpeg";
    }

    private async Task<Camera?> ResolverCameraAsync(Guid proprietarioId, Guid cameraId, CancellationToken cancellationToken)
    {
        var camera = await _cameras.GetByIdAsync(cameraId, cancellationToken).ConfigureAwait(false);
        return camera?.Propriedade is null || camera.Propriedade.ProprietarioId != proprietarioId ? null : camera;
    }

    /// <summary>Sprint 20 — cache-busting via querystring (ticks do último sucesso): sem isso, o cache de imagem do mobile (expo-image) nunca invalidaria depois de uma nova captura, porque a URL nunca mudaria.</summary>
    private static string MontarUrlImagem(Camera camera) =>
        $"/api/cameras/{camera.Id}/imagem?v={camera.UltimoSucessoCapturaUtc?.Ticks ?? 0}";

    private static CameraResponse ToResponse(Camera camera) => new()
    {
        Id = camera.Id,
        Nome = camera.Nome,
        Status = camera.Status,
        UltimaImagemUrl = camera.UltimoSnapshotPath is not null ? MontarUrlImagem(camera) : null,
        UltimaVezVistaUtc = camera.UltimoSucessoCapturaUtc,
    };

    /// <summary>Nunca expõe o texto técnico do provider (pode conter IP/porta/erro HTTP cru) — só as 2 categorias amigáveis que fazem sentido para o morador.</summary>
    private static string MapearMotivoAmigavel(string? erroTecnico)
    {
        if (string.IsNullOrEmpty(erroTecnico))
        {
            return "Não foi possível obter uma imagem da câmera agora.";
        }

        var texto = erroTecnico.ToLowerInvariant();
        if (texto.Contains("timeout") || texto.Contains("cancel"))
        {
            return "A câmera demorou demais para responder.";
        }

        return "Não foi possível obter uma imagem da câmera agora.";
    }
}
