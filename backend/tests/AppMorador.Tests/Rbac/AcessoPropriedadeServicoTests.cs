using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Rbac;

/// <summary>Sprint 25 (ADR 0041) — papéis e níveis de acesso por propriedade.</summary>
public class AcessoPropriedadeServicoTests
{
    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _membroId = Guid.NewGuid();
    private readonly Propriedade _propriedade;

    public AcessoPropriedadeServicoTests()
    {
        _propriedade = new Propriedade { Id = Guid.NewGuid(), Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = _donoId };
    }

    private async Task<AcessoPropriedadeServico> ComVinculo(PerfilPropriedade papel, bool ativo = true)
    {
        var repo = new Mock<IUsuarioPropriedadeRepositorio>();
        repo.Setup(r => r.ListAtivosByUsuarioAsync(_membroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ativo
                ? [new UsuarioPropriedade { UsuarioId = _membroId, PropriedadeId = _propriedade.Id, Perfil = papel, CreatedAtUtc = DateTime.UtcNow }]
                : []);
        var servico = new AcessoPropriedadeServico(repo.Object);
        await servico.CarregarAsync(_membroId, CancellationToken.None);
        return servico;
    }

    [Theory]
    [InlineData(NivelAcesso.Ver)]
    [InlineData(NivelAcesso.Operar)]
    [InlineData(NivelAcesso.Alterar)]
    [InlineData(NivelAcesso.Proprietario)]
    public void Dono_PodeTudo_MesmoSemVinculosCarregados(NivelAcesso nivel)
    {
        Assert.True(new AcessoPropriedadeServico().Pode(_donoId, _propriedade, nivel));
    }

    [Theory]
    [InlineData(PerfilPropriedade.Gerente, NivelAcesso.Alterar, true)]
    [InlineData(PerfilPropriedade.Gerente, NivelAcesso.Proprietario, false)]
    [InlineData(PerfilPropriedade.Morador, NivelAcesso.Operar, true)]
    [InlineData(PerfilPropriedade.Morador, NivelAcesso.Alterar, false)]
    [InlineData(PerfilPropriedade.Visualizador, NivelAcesso.Ver, true)]
    [InlineData(PerfilPropriedade.Visualizador, NivelAcesso.Operar, false)]
    public async Task Membro_PodeConformeOPapel(PerfilPropriedade papel, NivelAcesso nivel, bool esperado)
    {
        var acesso = await ComVinculo(papel);

        Assert.Equal(esperado, acesso.Pode(_membroId, _propriedade, nivel));
    }

    [Fact]
    public async Task VinculoInativo_NaoDaAcessoNenhum()
    {
        var acesso = await ComVinculo(PerfilPropriedade.Gerente, ativo: false);

        Assert.False(acesso.Pode(_membroId, _propriedade, NivelAcesso.Ver));
        Assert.Empty(acesso.PropriedadesComoMembro);
    }

    [Fact]
    public async Task VinculosCarregadosSaoSoDoUsuarioDaRequisicao()
    {
        var acesso = await ComVinculo(PerfilPropriedade.Gerente);

        Assert.False(acesso.Pode(Guid.NewGuid(), _propriedade, NivelAcesso.Ver));
    }
}
