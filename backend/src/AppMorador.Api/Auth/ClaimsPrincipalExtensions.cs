using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using AppMorador.Domain.Entities;

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

    /// <summary>Sprint 21 (ADR 0021) — null para todo cliente (a claim "role" só existe para os 3 papéis internos, ver JwtTokenService).</summary>
    public static RoleSistema? GetRoleGlobal(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("role");
        return value is not null && Enum.TryParse<RoleSistema>(value, out var role) ? role : null;
    }

    /// <summary>Interno da plataforma = tem RoleGlobal (Master/Tecnico/Suporte). Cliente = não tem.</summary>
    public static bool EhInterno(this ClaimsPrincipal principal) => principal.GetRoleGlobal() is not null;

    public static bool TemAlgumRoleGlobal(this ClaimsPrincipal principal, params RoleSistema[] roles) =>
        principal.GetRoleGlobal() is { } role && roles.Contains(role);

    public static bool EstaImpersonando(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("impersonating") == "true";

    public static Guid? GetImpersonadoPor(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("impersonatedBy");
        return value is not null && Guid.TryParse(value, out var id) ? id : null;
    }
}
