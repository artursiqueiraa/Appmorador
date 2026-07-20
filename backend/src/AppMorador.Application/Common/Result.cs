namespace AppMorador.Application.Common;

/// <summary>
/// Resultado de uma operacao de aplicacao que pode falhar por um motivo de negocio
/// esperado (nao encontrado, sem permissao, credenciais invalidas...). Evita usar
/// excecao para controle de fluxo em casos que nao sao excepcionais.
/// </summary>
public sealed class Result<T>
{
    public required bool Success { get; init; }

    public string? Error { get; init; }

    public T? Data { get; init; }

    public static Result<T> Ok(T data) => new() { Success = true, Data = data };

    public static Result<T> Fail(string error) => new() { Success = false, Error = error };
}
