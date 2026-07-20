using AppMorador.Application.Common;

namespace AppMorador.Application.Autenticacao;

public interface IAutenticacaoServico
{
    Task<Result<Guid>> RegisterAsync(CadastrarUsuarioRequest request, CancellationToken cancellationToken);

    Task<Result<EntrarResponse>> LoginAsync(EntrarRequest request, CancellationToken cancellationToken);

    Task<Result<EntrarResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}
