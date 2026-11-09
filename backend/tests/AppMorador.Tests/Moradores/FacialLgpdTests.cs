using AppMorador.Application.ControlId;
using AppMorador.Application.Equipamentos;
using AppMorador.Application.Hikvision;
using AppMorador.Application.Moradores;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Moradores;
using AppMorador.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AppMorador.Tests.Moradores;

/// <summary>Sprint 26 (ADR 0042) — consentimento biométrico e rosto nos leitores.</summary>
public class FacialLgpdTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0];
    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Morador _morador;

    public FacialLgpdTests()
    {
        var propriedade = new Propriedade { Id = Guid.NewGuid(), Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = _donoId };
        _morador = new Morador
        {
            Id = Guid.NewGuid(),
            Nome = "Ana",
            Status = StatusMorador.Ativo,
            CreatedAtUtc = DateTime.UtcNow,
            Unidade = new Unidade { Id = Guid.NewGuid(), Identificacao = "Casa", Tipo = TipoUnidade.Casa, CreatedAtUtc = DateTime.UtcNow, PropriedadeId = propriedade.Id, Propriedade = propriedade },
        };
    }

    private FotoMoradorServico Foto(ConsentimentoBiometrico? consentimento, Mock<IFacialLeitoresServico>? leitores = null)
    {
        var repo = new Mock<IMoradorRepositorio>();
        repo.Setup(r => r.GetByIdAsync(_morador.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_morador);
        var disco = new Mock<IArmazenamentoFotoMorador>();
        disco.Setup(d => d.SalvarAsync(_morador.Id, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("fotos/x.jpg");
        var consentimentos = new Mock<IConsentimentoBiometricoRepositorio>();
        consentimentos.Setup(c => c.GetAtivoAsync(_morador.Id, It.IsAny<CancellationToken>())).ReturnsAsync(consentimento);
        return new FotoMoradorServico(repo.Object, disco.Object, new AcessoPropriedadeServico(), consentimentos.Object, (leitores ?? new Mock<IFacialLeitoresServico>()).Object);
    }

    private ConsentimentoBiometrico Consentimento(bool responsavel = false) =>
        new() { Id = Guid.NewGuid(), MoradorId = _morador.Id, VersaoTermo = ConsentimentoBiometrico.VersaoTermoAtual, ResponsavelLegal = responsavel };

    [Fact]
    public async Task SemConsentimento_FotoRecusada()
    {
        var r = await Foto(null).DefinirAsync(_donoId, _morador.Id, Jpeg, CancellationToken.None);

        Assert.Equal("Registre o consentimento da pessoa antes de cadastrar o rosto.", r.Error);
    }

    [Fact]
    public async Task Dependente_SemResponsavelLegal_FotoRecusada()
    {
        _morador.Tipo = TipoMorador.Dependente;

        var r = await Foto(Consentimento(responsavel: false)).DefinirAsync(_donoId, _morador.Id, Jpeg, CancellationToken.None);

        Assert.False(r.Success);
    }

    [Fact]
    public async Task ComConsentimento_SalvaEEnviaAosLeitores()
    {
        var leitores = new Mock<IFacialLeitoresServico>();

        var r = await Foto(Consentimento(), leitores).DefinirAsync(_donoId, _morador.Id, Jpeg, CancellationToken.None);

        Assert.True(r.Success);
        leitores.Verify(l => l.SincronizarAsync(_morador, Jpeg, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoverFoto_ApagaDosLeitores()
    {
        _morador.FotoPath = "fotos/x.jpg";
        var leitores = new Mock<IFacialLeitoresServico>();

        await Foto(Consentimento(), leitores).RemoverAsync(_donoId, _morador.Id, CancellationToken.None);

        Assert.Null(_morador.FotoPath);
        leitores.Verify(l => l.SincronizarAsync(_morador, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Leitores_EnviaParaControlIdEHikvision_ERegistraFalha()
    {
        var controlId = new Equipamento { Id = Guid.NewGuid(), PropriedadeId = _morador.Unidade!.PropriedadeId, Nome = "Portaria", Fabricante = FabricanteEquipamento.ControlId, Status = StatusEquipamento.Online, Ip = "1.1.1.1", Porta = 80, Usuario = "a", SenhaCriptografada = "s", CreatedAtUtc = DateTime.UtcNow };
        var hik = new Equipamento { Id = Guid.NewGuid(), PropriedadeId = _morador.Unidade.PropriedadeId, Nome = "Garagem", Fabricante = FabricanteEquipamento.Hikvision, Status = StatusEquipamento.Online, Ip = "1.1.1.2", Porta = 80, Usuario = "a", SenhaCriptografada = "s", CreatedAtUtc = DateTime.UtcNow };
        var equipamentos = new Mock<IEquipamentoRepositorio>();
        equipamentos.Setup(e => e.ListByPropriedadeAsync(_morador.Unidade.PropriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync([controlId, hik]);
        var sinc = new Mock<ISincronizacaoFotoRepositorio>();
        sinc.Setup(s => s.ListByMoradorAsync(_morador.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var adicionados = new List<SincronizacaoFoto>();
        sinc.Setup(s => s.AddAsync(It.IsAny<SincronizacaoFoto>(), It.IsAny<CancellationToken>())).Callback<SincronizacaoFoto, CancellationToken>((s, _) => adicionados.Add(s));
        var cid = new Mock<IControlIdProvider>();
        cid.Setup(c => c.EnviarFotoAsync(It.IsAny<ConexaoEquipamento>(), _morador.Id, "Ana", Jpeg, It.IsAny<CancellationToken>())).ReturnsAsync(new ResultadoTesteConexao { Sucesso = true });
        var hv = new Mock<IHikvisionAcessoProvider>();
        hv.Setup(h => h.EnviarRostoAsync(It.IsAny<ConexaoEquipamento>(), _morador.Id, "Ana", Jpeg, It.IsAny<CancellationToken>())).ReturnsAsync(new ResultadoTesteConexao { Sucesso = false, MensagemErro = "fora do ar" });
        var cripto = new Mock<ICriptografiaSimetrica>();
        cripto.Setup(c => c.Descriptografar(It.IsAny<string>())).Returns("senha");

        var servico = new FacialLeitoresServico(equipamentos.Object, sinc.Object, new Mock<IMoradorRepositorio>().Object, new Mock<IArmazenamentoFotoMorador>().Object,
            cid.Object, hv.Object, cripto.Object, new Mock<IModeloEquipamentoRepositorio>().Object, NullLogger<FacialLeitoresServico>.Instance);
        await servico.SincronizarAsync(_morador, Jpeg, CancellationToken.None);

        Assert.Equal(SituacaoFotoLeitor.Enviada, adicionados.Single(a => a.EquipamentoId == controlId.Id).Situacao);
        var falha = adicionados.Single(a => a.EquipamentoId == hik.Id);
        Assert.Equal(SituacaoFotoLeitor.Falhou, falha.Situacao);
        Assert.Equal("fora do ar", falha.Mensagem);
    }
}
