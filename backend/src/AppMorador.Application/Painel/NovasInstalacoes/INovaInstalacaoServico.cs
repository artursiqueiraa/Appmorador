using AppMorador.Application.Common;

namespace AppMorador.Application.Painel.NovasInstalacoes;

public interface INovaInstalacaoServico
{
    Task<Result<NovaInstalacaoResponse>> CriarAsync(
        Guid usuarioId, CriarNovaInstalacaoRequest request, string? ipAddress, CancellationToken cancellationToken);
}
