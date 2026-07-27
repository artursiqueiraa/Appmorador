namespace AppMorador.Application.Rbac;

public sealed class ImpersonarRequest
{
    public required Guid PropriedadeId { get; init; }
}

public sealed class ImpersonarResponse
{
    public required string AccessToken { get; init; }

    public required int ExpiresInSeconds { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string PropriedadeNome { get; init; }

    public required string ClienteNome { get; init; }
}
