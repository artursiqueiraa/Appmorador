using System.ComponentModel.DataAnnotations;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using Xunit;

namespace AppMorador.Tests.Rbac;

/// <summary>DIVIDA_TECNICA 54 — a API de equipe interna precisa recusar senha fraca/vazia, igual ao cadastro público.</summary>
public class CriarUsuarioInternoRequestValidacaoTests
{
    private static List<ValidationResult> Validar(CriarUsuarioInternoRequest request)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), resultados, validateAllProperties: true);
        return resultados;
    }

    private static CriarUsuarioInternoRequest Pedido(string senha = "Senha123", string email = "ana@akteck.com") => new()
    {
        Nome = "Ana",
        Email = email,
        Senha = senha,
        RoleGlobal = RoleSistema.Suporte,
    };

    [Fact]
    public void PedidoValido_SemErros() => Assert.Empty(Validar(Pedido()));

    [Theory]
    [InlineData("")]
    [InlineData("curta1A")]
    [InlineData("semnumeroAA")]
    [InlineData("SEMMINUSCULA1")]
    [InlineData("semmaiuscula1")]
    public void SenhaFraca_Recusada(string senha) =>
        Assert.Contains(Validar(Pedido(senha)), r => r.MemberNames.Contains(nameof(CriarUsuarioInternoRequest.Senha)));

    [Fact]
    public void EmailInvalido_Recusado() =>
        Assert.Contains(Validar(Pedido(email: "nao-e-email")), r => r.MemberNames.Contains(nameof(CriarUsuarioInternoRequest.Email)));
}
