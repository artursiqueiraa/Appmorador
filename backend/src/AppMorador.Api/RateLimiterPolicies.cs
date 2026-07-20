namespace AppMorador.Api;

public static class RateLimiterPolicies
{
    /// <summary>Politica de rate limit aplicada a register/login — mitiga brute force e enumeration em massa.</summary>
    public const string Auth = "auth";
}
