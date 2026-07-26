namespace AppMorador.Domain.Entities;

/// <summary>
/// "Kill switch" geral da credencial — independente de quantos Pontos de Acesso ela
/// tenha permissao (ver <see cref="PermissaoAcesso"/>). Revogar aqui invalida o
/// acesso a todos os pontos de uma vez (ex.: cartao fisico perdido).
/// </summary>
public enum StatusCredencial
{
    Ativa,
    Suspensa,
    Expirada,
    Revogada,
}
