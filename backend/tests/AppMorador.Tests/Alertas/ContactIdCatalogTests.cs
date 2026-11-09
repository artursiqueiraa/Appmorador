using AppMorador.Domain.ContactId;
using Xunit;

namespace AppMorador.Tests.Alertas;

public class ContactIdCatalogTests
{
    [Fact]
    public void Codigo1130_ContinuaDisparoComZona_EUnicoJaHomologado()
    {
        Assert.True(ContactIdCatalog.TryGet("1130", out var definicao));
        Assert.Equal(CategoriaContactId.Disparo, definicao!.Categoria);
        Assert.True(definicao.GeneratesOccurrence);
        Assert.True(definicao.UsaZona);
        Assert.False(definicao.PendenteHomologacao);
        Assert.Equal("Movimento detectado", definicao.FriendlyMessage);
    }

    [Theory]
    [InlineData("1120", CategoriaContactId.Panico)]
    [InlineData("1121", CategoriaContactId.Panico)]
    [InlineData("1137", CategoriaContactId.Violacao)]
    [InlineData("1301", CategoriaContactId.FalhaEnergia)]
    [InlineData("1302", CategoriaContactId.BateriaBaixa)]
    [InlineData("3301", CategoriaContactId.Restauracao)]
    public void CodigosSiaDc05_TemCategoriaEFicamPendentesDeHomologacao(string codigo, CategoriaContactId categoria)
    {
        Assert.True(ContactIdCatalog.TryGet(codigo, out var definicao));
        Assert.Equal(categoria, definicao!.Categoria);
        Assert.True(definicao.PendenteHomologacao);
    }

    [Theory]
    [InlineData("1120")]
    [InlineData("1301")]
    [InlineData("1302")]
    [InlineData("1137")]
    public void EventosDoSistema_NaoUsamZona(string codigo)
    {
        Assert.True(ContactIdCatalog.TryGet(codigo, out var definicao));
        Assert.False(definicao!.UsaZona);
    }

    [Fact]
    public void RestauracaoDeZonaDeIntrusao_NaoGeraOcorrencia()
    {
        Assert.True(ContactIdCatalog.TryGet("3130", out var definicao));
        Assert.False(definicao!.GeneratesOccurrence);
    }

    [Fact]
    public void CodigoForaDoCatalogo_ContinuaDesconhecido()
    {
        Assert.False(ContactIdCatalog.TryGet("1999", out _));
    }

    [Fact]
    public void SoDisparoPanicoEViolacao_SaoCriticos()
    {
        Assert.True(ContactIdCatalog.EhCritica(CategoriaContactId.Disparo));
        Assert.True(ContactIdCatalog.EhCritica(CategoriaContactId.Panico));
        Assert.True(ContactIdCatalog.EhCritica(CategoriaContactId.Violacao));
        Assert.False(ContactIdCatalog.EhCritica(CategoriaContactId.FalhaEnergia));
        Assert.False(ContactIdCatalog.EhCritica(CategoriaContactId.BateriaBaixa));
        Assert.False(ContactIdCatalog.EhCritica(CategoriaContactId.Restauracao));
    }

    [Fact]
    public void CodigosDaCategoria_DevolveSoOsDaCategoriaPedida()
    {
        var restauracoes = ContactIdCatalog.CodigosDaCategoria(CategoriaContactId.Restauracao);
        Assert.Contains("3301", restauracoes);
        Assert.DoesNotContain("1130", restauracoes);
    }
}
