using AppMorador.Application.Rbac;
using AppMorador.Application.ContatosEmergencia;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Alertas;

public class ContatoEmergenciaServicoTests
{
    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Propriedade _propriedade;
    private readonly Mock<IContatoEmergenciaRepositorio> _contatos = new();
    private readonly Mock<IPropriedadeRepositorio> _propriedades = new();
    private readonly ContatoEmergenciaServico _servico;

    public ContatoEmergenciaServicoTests()
    {
        _propriedade = new Propriedade { Id = Guid.NewGuid(), Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = _donoId };
        _propriedades.Setup(p => p.GetByIdAsync(_propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_propriedade);
        _servico = new ContatoEmergenciaServico(_contatos.Object, _propriedades.Object, new AcessoPropriedadeServico());
    }

    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("11 3456-7890", "1134567890")]
    [InlineData("+55 11 98765-4321", "5511987654321")]
    public void Validar_TelefoneComDdd_GuardaSoDigitos(string entrada, string esperado)
    {
        var erro = ContatoEmergenciaServico.Validar(new SalvarContatoEmergenciaRequest { Nome = "Ana", Telefone = entrada }, out _, out var telefone);

        Assert.Null(erro);
        Assert.Equal(esperado, telefone);
    }

    [Theory]
    [InlineData("98765-4321")]
    [InlineData("")]
    [InlineData("1234567890123")]
    public void Validar_TelefoneSemDdd_Recusa(string entrada)
    {
        var erro = ContatoEmergenciaServico.Validar(new SalvarContatoEmergenciaRequest { Nome = "Ana", Telefone = entrada }, out _, out _);

        Assert.Equal("Informe o telefone com DDD.", erro);
    }

    [Fact]
    public async Task Criar_EntraNoFimDaOrdem()
    {
        _contatos.Setup(c => c.ListByPropriedadeAsync(_propriedade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ContatoEmergencia { Id = Guid.NewGuid(), Nome = "Ana", Telefone = "11987654321", Ordem = 1 }]);

        var result = await _servico.CriarAsync(_donoId, _propriedade.Id, new SalvarContatoEmergenciaRequest { Nome = "Beto", Telefone = "11912345678" }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Ordem);
    }

    [Fact]
    public async Task Criar_AlemDoLimite_Recusa()
    {
        var existentes = Enumerable.Range(1, ContatoEmergenciaServico.LimitePorPropriedade)
            .Select(i => new ContatoEmergencia { Id = Guid.NewGuid(), Nome = $"C{i}", Telefone = "11987654321", Ordem = i })
            .ToList();
        _contatos.Setup(c => c.ListByPropriedadeAsync(_propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existentes);

        var result = await _servico.CriarAsync(_donoId, _propriedade.Id, new SalvarContatoEmergenciaRequest { Nome = "Extra", Telefone = "11912345678" }, CancellationToken.None);

        Assert.False(result.Success);
        _contatos.Verify(c => c.AddAsync(It.IsAny<ContatoEmergencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Criar_QuemNaoEDono_Recusa()
    {
        var result = await _servico.CriarAsync(Guid.NewGuid(), _propriedade.Id, new SalvarContatoEmergenciaRequest { Nome = "Ana", Telefone = "11987654321" }, CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Reordenar_AplicaANovaOrdem()
    {
        var a = new ContatoEmergencia { Id = Guid.NewGuid(), Nome = "Ana", Telefone = "11987654321", Ordem = 1, PropriedadeId = _propriedade.Id };
        var b = new ContatoEmergencia { Id = Guid.NewGuid(), Nome = "Beto", Telefone = "11912345678", Ordem = 2, PropriedadeId = _propriedade.Id };
        _contatos.Setup(c => c.ListByPropriedadeAsync(_propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync([a, b]);

        var result = await _servico.ReordenarAsync(_donoId, _propriedade.Id, new ReordenarContatosEmergenciaRequest { Ids = [b.Id, a.Id] }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, b.Ordem);
        Assert.Equal(2, a.Ordem);
    }

    [Fact]
    public async Task Remover_FazSoftDelete()
    {
        var contato = new ContatoEmergencia { Id = Guid.NewGuid(), Nome = "Ana", Telefone = "11987654321", Ordem = 1, PropriedadeId = _propriedade.Id };
        _contatos.Setup(c => c.GetByIdAsync(contato.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contato);

        var result = await _servico.RemoverAsync(_donoId, contato.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(contato.Excluido);
        Assert.Equal(_donoId, contato.ExcluidoPorUsuarioId);
    }
}
