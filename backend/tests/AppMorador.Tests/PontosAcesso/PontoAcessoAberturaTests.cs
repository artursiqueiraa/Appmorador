using AppMorador.Application.Rbac;
using AppMorador.Application.Common;
using AppMorador.Application.ControlId;
using AppMorador.Application.Equipamentos;
using AppMorador.Application.Jfl;
using AppMorador.Application.PontosAcesso;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.PontosAcesso;

/// <summary>Sprint 24.5 — abrir porta pelo ponto de acesso e vincular acionador/câmera.</summary>
public class PontoAcessoAberturaTests
{
    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Propriedade _propriedade;
    private readonly Mock<IPropriedadeRepositorio> _propriedades = new();
    private readonly Mock<IPontoAcessoRepositorio> _pontos = new();
    private readonly Mock<IEquipamentoRepositorio> _equipamentos = new();
    private readonly Mock<ICameraRepositorio> _cameras = new();
    private readonly Mock<IJflComandoServico> _jfl = new();
    private readonly Mock<IControlIdProvider> _controlId = new();
    private readonly Mock<ICriptografiaSimetrica> _cripto = new();
    private readonly PontoAcessoServico _servico;

    public PontoAcessoAberturaTests()
    {
        _propriedade = new Propriedade { Id = Guid.NewGuid(), Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = _donoId };
        _propriedades.Setup(p => p.GetByIdAsync(_propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_propriedade);
        _cripto.Setup(c => c.Descriptografar(It.IsAny<string>())).Returns("senha");
        _servico = new PontoAcessoServico(
            _propriedades.Object, _pontos.Object, new Mock<IPermissaoAcessoRepositorio>().Object, new Mock<IPermissaoVeicularRepositorio>().Object,
            _equipamentos.Object, _cameras.Object, _jfl.Object, _controlId.Object, new Mock<AppMorador.Application.Hikvision.IHikvisionAcessoProvider>().Object, _cripto.Object, new AcessoPropriedadeServico());
    }

    private Equipamento Equipamento(FabricanteEquipamento fabricante, Guid? propriedadeId = null) => new()
    {
        Id = Guid.NewGuid(),
        PropriedadeId = propriedadeId ?? _propriedade.Id,
        Nome = "Equipamento",
        Fabricante = fabricante,
        Status = StatusEquipamento.Online,
        Ip = "10.0.0.5",
        Porta = 80,
        Usuario = "admin",
        SenhaCriptografada = "x",
        CreatedAtUtc = DateTime.UtcNow,
    };

    private PontoAcesso Ponto(Equipamento? acionador, int? saida) => new()
    {
        Id = Guid.NewGuid(),
        PropriedadeId = _propriedade.Id,
        Propriedade = _propriedade,
        Nome = "Portão",
        Tipo = TipoPontoAcesso.Geral,
        CreatedAtUtc = DateTime.UtcNow,
        EquipamentoAcionador = acionador,
        EquipamentoAcionadorId = acionador?.Id,
        NumeroSaida = saida,
    };

    [Fact]
    public async Task Abrir_PelaCentralJfl_UsaComandoPgm()
    {
        var central = Equipamento(FabricanteEquipamento.Jfl);
        var ponto = Ponto(central, 2);
        _pontos.Setup(p => p.GetByIdAsync(ponto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ponto);
        _jfl.Setup(j => j.AcionarPgmAsync(_donoId, central.Id, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ResultadoComandoJfl>.Ok(new ResultadoComandoJfl { Sucesso = true }));

        var result = await _servico.AbrirAsync(_donoId, ponto.Id, CancellationToken.None);

        Assert.True(result.Data!.Sucesso);
    }

    [Fact]
    public async Task Abrir_PeloLeitorControlId_ChamaAbrirPortaNaPortaCerta()
    {
        var leitor = Equipamento(FabricanteEquipamento.ControlId);
        var ponto = Ponto(leitor, 1);
        _pontos.Setup(p => p.GetByIdAsync(ponto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ponto);
        _controlId.Setup(c => c.AbrirPortaAsync(It.IsAny<ConexaoEquipamento>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoTesteConexao { Sucesso = false, MensagemErro = "Tempo esgotado ao tentar conectar ao equipamento." });

        var result = await _servico.AbrirAsync(_donoId, ponto.Id, CancellationToken.None);

        Assert.False(result.Data!.Sucesso);
        Assert.Contains("Tempo esgotado", result.Data.Mensagem);
    }

    [Fact]
    public async Task Abrir_SemAcionador_ExplicaComoLigar()
    {
        var ponto = Ponto(null, null);
        _pontos.Setup(p => p.GetByIdAsync(ponto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ponto);

        var result = await _servico.AbrirAsync(_donoId, ponto.Id, CancellationToken.None);

        Assert.False(result.Data!.Sucesso);
        Assert.Contains("não está ligada", result.Data.Mensagem);
    }

    [Fact]
    public async Task Abrir_QuemNaoEDono_NaoEncontra()
    {
        var ponto = Ponto(Equipamento(FabricanteEquipamento.Jfl), 1);
        _pontos.Setup(p => p.GetByIdAsync(ponto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ponto);

        var result = await _servico.AbrirAsync(Guid.NewGuid(), ponto.Id, CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Criar_ComEquipamentoDeOutraPropriedade_Recusa()
    {
        var deFora = Equipamento(FabricanteEquipamento.Jfl, Guid.NewGuid());
        _equipamentos.Setup(e => e.GetByIdAsync(deFora.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deFora);

        var result = await _servico.CreateAsync(_donoId, _propriedade.Id,
            new CriarPontoAcessoRequest { Nome = "Portão", EquipamentoAcionadorId = deFora.Id, NumeroSaida = 1 }, CancellationToken.None);

        Assert.False(result.Success);
        _pontos.Verify(p => p.AddAsync(It.IsAny<PontoAcesso>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Criar_ComAcionadorSemSaida_PedeASaida()
    {
        var central = Equipamento(FabricanteEquipamento.Jfl);
        _equipamentos.Setup(e => e.GetByIdAsync(central.Id, It.IsAny<CancellationToken>())).ReturnsAsync(central);

        var result = await _servico.CreateAsync(_donoId, _propriedade.Id,
            new CriarPontoAcessoRequest { Nome = "Portão", EquipamentoAcionadorId = central.Id }, CancellationToken.None);

        Assert.Equal("Informe qual saída do equipamento abre esta porta.", result.Error);
    }

    [Fact]
    public async Task Criar_ComAcionadorECamera_DevolvePodeAbrirENomes()
    {
        var central = Equipamento(FabricanteEquipamento.Jfl);
        var camera = new Camera { Id = Guid.NewGuid(), PropriedadeId = _propriedade.Id, Canal = 1, Nome = "Câmera do portão" };
        _equipamentos.Setup(e => e.GetByIdAsync(central.Id, It.IsAny<CancellationToken>())).ReturnsAsync(central);
        _cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        var result = await _servico.CreateAsync(_donoId, _propriedade.Id,
            new CriarPontoAcessoRequest { Nome = "Portão", EquipamentoAcionadorId = central.Id, NumeroSaida = 1, CameraId = camera.Id }, CancellationToken.None);

        Assert.True(result.Data!.PodeAbrir);
        Assert.Equal("Câmera do portão", result.Data.NomeCamera);
        Assert.Equal(FabricanteEquipamento.Jfl, result.Data.FabricanteAcionador);
    }
}
