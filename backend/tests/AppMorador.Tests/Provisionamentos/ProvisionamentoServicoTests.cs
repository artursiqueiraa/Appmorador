using AppMorador.Application.Provisionamentos;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Provisionamentos;

public class ProvisionamentoServicoTests
{
    private static (Mock<IPropriedadeRepositorio> Propriedades, Mock<IProvisionamentoRepositorio> Provisionamentos, ProvisionamentoServico Servico) NovoServico()
    {
        var propriedades = new Mock<IPropriedadeRepositorio>();
        var provisionamentos = new Mock<IProvisionamentoRepositorio>();
        var servico = new ProvisionamentoServico(propriedades.Object, provisionamentos.Object);
        return (propriedades, provisionamentos, servico);
    }

    [Fact]
    public async Task CriarAsync_PropriedadeInexistente_Falha()
    {
        var (propriedades, _, servico) = NovoServico();
        propriedades.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Propriedade?)null);

        var resultado = await servico.CriarAsync(Guid.NewGuid(), new CriarProvisionamentoRequest { Nome = "Instalação Inicial", Template = TemplateProvisionamento.Residencia }, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task CriarAsync_Sucesso_NasceComoRascunho()
    {
        var (propriedades, provisionamentos, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Propriedade { Id = propriedadeId, Nome = "Loja Centro", Tipo = TipoPropriedade.Comercial, ProprietarioId = Guid.NewGuid() });

        var resultado = await servico.CriarAsync(propriedadeId, new CriarProvisionamentoRequest { Nome = "Instalação Loja", Template = TemplateProvisionamento.Loja }, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(StatusProvisionamento.Rascunho, resultado.Data!.Status);
        provisionamentos.Verify(p => p.AddAsync(It.IsAny<Provisionamento>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArquivarAsync_ProvisionamentoInexistente_Falha()
    {
        var (_, provisionamentos, servico) = NovoServico();
        provisionamentos.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Provisionamento?)null);

        var resultado = await servico.ArquivarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task ArquivarAsync_Sucesso_MudaStatusParaArquivado()
    {
        var (_, provisionamentos, servico) = NovoServico();
        var id = Guid.NewGuid();
        var provisionamento = new Provisionamento { Id = id, PropriedadeId = Guid.NewGuid(), Nome = "Instalação", Template = TemplateProvisionamento.Escritorio, Status = StatusProvisionamento.Ativo, CreatedAtUtc = DateTime.UtcNow };
        provisionamentos.Setup(p => p.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(provisionamento);

        var resultado = await servico.ArquivarAsync(id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(StatusProvisionamento.Arquivado, provisionamento.Status);
        Assert.NotNull(provisionamento.AtualizadoEmUtc);
    }
}
