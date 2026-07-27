using System.Security.Claims;
using AppMorador.Api.Auth;
using AppMorador.Domain.Entities;
using Xunit;

namespace AppMorador.Tests.Rbac;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal NovoPrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test"));

    [Fact]
    public void GetRoleGlobal_SemClaim_RetornaNull()
    {
        var principal = NovoPrincipal();

        Assert.Null(principal.GetRoleGlobal());
    }

    [Fact]
    public void GetRoleGlobal_ComClaimValida_RetornaRole()
    {
        var principal = NovoPrincipal(new Claim("role", "Master"));

        Assert.Equal(RoleSistema.Master, principal.GetRoleGlobal());
    }

    [Fact]
    public void EhInterno_SemClaimRole_RetornaFalse()
    {
        var principal = NovoPrincipal();

        Assert.False(principal.EhInterno());
    }

    [Fact]
    public void EhInterno_ComClaimRole_RetornaTrue()
    {
        var principal = NovoPrincipal(new Claim("role", "Tecnico"));

        Assert.True(principal.EhInterno());
    }

    [Fact]
    public void TemAlgumRoleGlobal_RoleForaDaLista_RetornaFalse()
    {
        var principal = NovoPrincipal(new Claim("role", "Tecnico"));

        Assert.False(principal.TemAlgumRoleGlobal(RoleSistema.Master, RoleSistema.Suporte));
    }

    [Fact]
    public void TemAlgumRoleGlobal_RoleNaLista_RetornaTrue()
    {
        var principal = NovoPrincipal(new Claim("role", "Suporte"));

        Assert.True(principal.TemAlgumRoleGlobal(RoleSistema.Master, RoleSistema.Suporte));
    }

    [Fact]
    public void EstaImpersonando_SemClaim_RetornaFalse()
    {
        var principal = NovoPrincipal();

        Assert.False(principal.EstaImpersonando());
    }

    [Fact]
    public void EstaImpersonando_ComClaimTrue_RetornaTrue()
    {
        var principal = NovoPrincipal(new Claim("impersonating", "true"));

        Assert.True(principal.EstaImpersonando());
    }

    [Fact]
    public void GetImpersonadoPor_SemClaim_RetornaNull()
    {
        var principal = NovoPrincipal();

        Assert.Null(principal.GetImpersonadoPor());
    }

    [Fact]
    public void GetImpersonadoPor_ComClaimValida_RetornaGuid()
    {
        var masterId = Guid.NewGuid();
        var principal = NovoPrincipal(new Claim("impersonatedBy", masterId.ToString()));

        Assert.Equal(masterId, principal.GetImpersonadoPor());
    }
}
