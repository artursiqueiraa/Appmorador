using AppMorador.Application.Common;

namespace AppMorador.Application.Credenciais;

public interface ICredencialServico
{
    Task<Result<CredencialResponse>> CreateAsync(Guid proprietarioId, Guid moradorId, CriarCredencialRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<CredencialResponse>>> ListByMoradorAsync(Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken);

    Task<Result<CredencialResponse>> AtualizarStatusAsync(
        Guid proprietarioId, Guid credencialId, AtualizarStatusCredencialRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid credencialId, CancellationToken cancellationToken);
}
