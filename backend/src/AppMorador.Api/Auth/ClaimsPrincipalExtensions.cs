using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AppMorador.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUsuarioId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token sem claim de usuario (sub).");

        return Guid.Parse(value);
    }
}
