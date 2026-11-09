using AppMorador.Application.Rbac;
using AppMorador.Application.Moradores;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Moradores;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Moradores;

public class FotoMoradorServicoTests
{
    private static readonly Guid DonoId = Guid.NewGuid();
    private static readonly Guid MoradorId = Guid.NewGuid();
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

    private static Morador NovoMorador(Guid dono, string? foto = null) => new()
    {
        Id = MoradorId,
        Nome = "Ana",
        Status = StatusMorador.Ativo,
        CreatedAtUtc = DateTime.UtcNow,
        FotoPath = foto,
        Unidade = new Unidade
        {
            Id = Guid.NewGuid(),
            Identificacao = "Casa",
            Tipo = TipoUnidade.Casa,
            CreatedAtUtc = DateTime.UtcNow,
            Propriedade = new Propriedade { Id = Guid.NewGuid(), Nome = "P", Tipo = TipoPropriedade.Residencial, ProprietarioId = dono },
        },
    };

    private static (Mock<IMoradorRepositorio> Repo, Mock<IArmazenamentoFotoMorador> Disco, FotoMoradorServico Servico) Montar(Morador? morador)
    {
        var repo = new Mock<IMoradorRepositorio>();
        repo.Setup(r => r.GetByIdAsync(MoradorId, It.IsAny<CancellationToken>())).ReturnsAsync(morador);
        var disco = new Mock<IArmazenamentoFotoMorador>();
        disco.Setup(d => d.SalvarAsync(MoradorId, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, byte[] _, string ext, CancellationToken _) => $"fotos-moradores/{MoradorId}/nova.{ext}");
        // Sprint 26 — os cenários existentes partem de consentimento já registrado.
        var consentimentos = new Mock<IConsentimentoBiometricoRepositorio>();
        consentimentos.Setup(c => c.GetAtivoAsync(MoradorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsentimentoBiometrico { Id = Guid.NewGuid(), MoradorId = MoradorId, VersaoTermo = "v", ResponsavelLegal = true });
        return (repo, disco, new FotoMoradorServico(repo.Object, disco.Object, new AcessoPropriedadeServico(), consentimentos.Object, new Mock<IFacialLeitoresServico>().Object));
    }

    [Fact]
    public async Task Definir_JpegValido_SalvaEApagaAFotoAnterior()
    {
        var morador = NovoMorador(DonoId, foto: "fotos-moradores/antiga.jpg");
        var (repo, disco, servico) = Montar(morador);

        var resultado = await servico.DefinirAsync(DonoId, MoradorId, Jpeg, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.EndsWith("nova.jpg", morador.FotoPath);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        disco.Verify(d => d.Remover("fotos-moradores/antiga.jpg"), Times.Once);
    }

    [Fact]
    public async Task Definir_Png_GravaComExtensaoPng()
    {
        var morador = NovoMorador(DonoId);
        var (_, disco, servico) = Montar(morador);

        await servico.DefinirAsync(DonoId, MoradorId, Png, CancellationToken.None);

        disco.Verify(d => d.SalvarAsync(MoradorId, It.IsAny<byte[]>(), "png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Definir_ArquivoQueNaoEImagem_RecusadoSemGravar()
    {
        var (_, disco, servico) = Montar(NovoMorador(DonoId));

        var resultado = await servico.DefinirAsync(DonoId, MoradorId, "<html>"u8.ToArray(), CancellationToken.None);

        Assert.False(resultado.Success);
        disco.Verify(d => d.SalvarAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Definir_MoradorDeOutroDono_MesmaMensagemDeNaoEncontrado()
    {
        var (_, disco, servico) = Montar(NovoMorador(Guid.NewGuid()));

        var resultado = await servico.DefinirAsync(DonoId, MoradorId, Jpeg, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal("Morador não encontrado.", resultado.Error);
        disco.Verify(d => d.SalvarAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Obter_SemFoto_Falha()
    {
        var (_, _, servico) = Montar(NovoMorador(DonoId));

        var resultado = await servico.ObterAsync(DonoId, MoradorId, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task Remover_LimpaCaminhoEApagaArquivo()
    {
        var morador = NovoMorador(DonoId, foto: "fotos-moradores/x.jpg");
        var (_, disco, servico) = Montar(morador);

        var resultado = await servico.RemoverAsync(DonoId, MoradorId, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Null(morador.FotoPath);
        disco.Verify(d => d.Remover("fotos-moradores/x.jpg"), Times.Once);
    }
}
