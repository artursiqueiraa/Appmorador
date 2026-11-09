namespace AppMorador.Application.Conta;

/// <summary>Sprint 25 (ADR 0041) — encerra todas as sessões (refresh tokens) de um usuário.</summary>
public interface ISessoesUsuario
{
    Task EncerrarTodasAsync(Guid usuarioId, CancellationToken cancellationToken);
}
