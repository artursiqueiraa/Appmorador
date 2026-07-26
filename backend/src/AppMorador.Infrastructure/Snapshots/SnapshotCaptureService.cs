using AppMorador.Domain.Snapshots;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Snapshots;

/// <summary>
/// Orquestra a captura: recebe de <see cref="ICameraResolver"/> a Camera/Gravador ja
/// resolvida para a zona, escolhe o ISnapshotProvider pelo fabricante, captura, e
/// grava via ISnapshotStorage. Nao consulta o banco diretamente (isso e
/// responsabilidade exclusiva do resolver) nem contem lógica de negocio do evento
/// JFL em si (isso e do <see cref="AppMorador.Infrastructure.Jfl.AlarmEventProcessor"/>,
/// que apenas chama este servico depois de criar a Ocorrencia).
/// </summary>
public sealed class SnapshotCaptureService
{
    private readonly ICameraResolver _cameraResolver;
    private readonly IEnumerable<ISnapshotProvider> _providers;
    private readonly ISnapshotStorage _storage;
    private readonly ILogger<SnapshotCaptureService> _logger;

    public SnapshotCaptureService(
        ICameraResolver cameraResolver,
        IEnumerable<ISnapshotProvider> providers,
        ISnapshotStorage storage,
        ILogger<SnapshotCaptureService> logger)
    {
        _cameraResolver = cameraResolver;
        _providers = providers;
        _storage = storage;
        _logger = logger;
    }

    public async Task<SnapshotResult> CapturarAsync(Guid propriedadeId, Guid zonaId, DateTime recebidoEmUtc, CancellationToken cancellationToken)
    {
        var camera = await _cameraResolver.ResolveAsync(zonaId, cancellationToken).ConfigureAwait(false);

        if (camera?.Gravador is null)
        {
            return SnapshotResult.Fail("Nenhuma camera vinculada a esta zona (VinculoZonaCamera inexistente).");
        }

        var gravador = camera.Gravador;
        var provider = _providers.FirstOrDefault(p => p.Fabricante == gravador.Fabricante);
        if (provider is null)
        {
            return SnapshotResult.Fail($"Nenhum ISnapshotProvider registrado para o fabricante {gravador.Fabricante}.");
        }

        var request = new SnapshotRequest
        {
            Ip = gravador.Ip,
            Porta = gravador.Porta,
            Username = gravador.NomeAcesso,
            Password = gravador.Senha,
            Canal = camera.Canal,
            CancellationToken = cancellationToken,
        };

        byte[]? bytes;
        try
        {
            bytes = await provider.CaptureAsync(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao capturar snapshot (DVR {Ip}:{Porta}, canal {Canal})", gravador.Ip, gravador.Porta, camera.Canal);
            return SnapshotResult.Fail(ex.Message);
        }

        if (bytes is null || bytes.Length == 0)
        {
            return SnapshotResult.Fail("Snapshot nao obtido (timeout, autenticacao falhou, ou resposta HTTP nao-2xx).");
        }

        var path = await _storage.SaveAsync(propriedadeId, recebidoEmUtc, bytes, cancellationToken).ConfigureAwait(false);
        return SnapshotResult.Ok(path);
    }
}
