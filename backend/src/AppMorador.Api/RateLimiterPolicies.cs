namespace AppMorador.Api;

public static class RateLimiterPolicies
{
    /// <summary>Politica de rate limit aplicada a register/login — mitiga brute force e enumeration em massa.</summary>
    public const string Auth = "auth";

    /// <summary>Sprint 14 — politica aplicada ao endpoint de negotiate/conexao do OperacionalHub, mitigando abertura em massa de conexoes SignalR.</summary>
    public const string Realtime = "realtime";
}
