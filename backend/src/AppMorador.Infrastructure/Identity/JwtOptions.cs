namespace AppMorador.Infrastructure.Identity;

/// <summary>
/// Nunca preencher `Key` no appsettings.json committado — vem de user-secrets em
/// dev (`dotnet user-secrets set "Jwt:Key" "..."`) ou variavel de ambiente
/// (`Jwt__Key`) em producao.
/// </summary>
public sealed class JwtOptions
{
    public required string Key { get; set; }

    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public int AccessTokenMinutes { get; set; } = 20;

    public int RefreshTokenDays { get; set; } = 30;
}
