using AppMorador.Application.ControlId;
using AppMorador.Application.Equipamentos;
using AppMorador.Application.Intelbras;
using AppMorador.Application.Painel.Equipamentos;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Painel;

public class EquipamentoAdminServicoTests
{
    private static (
        Mock<IEquipamentoRepositorio> Equipamentos, Mock<IPropriedadeRepositorio> Propriedades,
        Mock<IModeloEquipamentoRepositorio> Modelos, Mock<IControlIdProvider> ControlId,
        Mock<IIntelbrasProvider> Intelbras, Mock<ICriptografiaSimetrica> Criptografia, EquipamentoAdminServico Servico)
        NovoServico()
    {
        var equipamentos = new Mock<IEquipamentoRepositorio>();
        var propriedades = new Mock<IPropriedadeRepositorio>();
        var modelos = new Mock<IModeloEquipamentoRepositorio>();
        var controlId = new Mock<IControlIdProvider>();
        var intelbras = new Mock<IIntelbrasProvider>();
        var criptografia = new Mock<ICriptografiaSimetrica>();
        criptografia.Setup(c => c.Criptografar(It.IsAny<string>())).Returns<string>(s => $"enc:{s}");

        var servico = new EquipamentoAdminServico(
            equipamentos.Object, propriedades.Object, modelos.Object, controlId.Object, intelbras.Object, criptografia.Object);

        return (equipamentos, propriedades, modelos, controlId, intelbras, criptografia, servico);
    }

    private static Propriedade NovaPropriedade(Guid id) =>
        new() { Id = id, Nome = "Casa Teste", Tipo = TipoPropriedade.Residencial, ProprietarioId = Guid.NewGuid() };

    private static CriarEquipamentoAdminRequest NovaRequisicaoJfl(
        Guid propriedadeId, string numeroSerie = "SN-001", string? modelo = null,
        string? ip = null, int? porta = null, string? usuario = null, string? senha = null) => new()
    {
        PropriedadeId = propriedadeId,
        Nome = "Central Teste",
        Fabricante = FabricanteEquipamento.Jfl,
        Modelo = modelo,
        NumeroSerie = numeroSerie,
        EstadoOperacional = EstadoOperacionalEquipamento.Ativo,
        Ip = ip,
        Porta = porta,
        Usuario = usuario,
        Senha = senha,
    };

    private static CriarEquipamentoAdminRequest NovaRequisicaoControlId(
        Guid propriedadeId, string? ip = "192.168.1.50", int? porta = 80, string? usuario = "admin", string? senha = "admin123") => new()
    {
        PropriedadeId = propriedadeId,
        Nome = "Catraca Teste",
        Fabricante = FabricanteEquipamento.ControlId,
        EstadoOperacional = EstadoOperacionalEquipamento.Ativo,
        Ip = ip,
        Porta = porta,
        Usuario = usuario,
        Senha = senha,
    };

    private static CriarEquipamentoAdminRequest NovaRequisicaoIntelbras(
        Guid propriedadeId, string? ip = "192.168.1.60", int? porta = 9009, string? senha = "1234") => new()
    {
        PropriedadeId = propriedadeId,
        Nome = "Central Intelbras Teste",
        Fabricante = FabricanteEquipamento.Intelbras,
        EstadoOperacional = EstadoOperacionalEquipamento.Ativo,
        Ip = ip,
        Porta = porta,
        Senha = senha,
    };

    [Fact]
    public async Task CriarAsync_PropriedadeInexistente_Falha()
    {
        var (_, propriedades, _, _, _, _, servico) = NovoServico();
        propriedades.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Propriedade?)null);

        var resultado = await servico.CriarAsync(NovaRequisicaoJfl(Guid.NewGuid()), CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task CriarAsync_NumeroSerieDuplicadoNaPropriedade_Falha()
    {
        var (equipamentos, propriedades, _, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        equipamentos.Setup(e => e.ExisteNumeroSerieDuplicadoAsync(propriedadeId, "SN-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await servico.CriarAsync(NovaRequisicaoJfl(propriedadeId), CancellationToken.None);

        Assert.False(resultado.Success);
        equipamentos.Verify(e => e.AddAsync(It.IsAny<Equipamento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_Jfl_Sucesso_NovoEquipamentoComeçaDesconhecidoENuncaOnlineOffline()
    {
        var (equipamentos, propriedades, modelos, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        equipamentos.Setup(e => e.ExisteNumeroSerieDuplicadoAsync(propriedadeId, "SN-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        modelos.Setup(m => m.GetByFabricanteENomeAsync(It.IsAny<FabricanteEquipamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModeloEquipamento?)null);

        var resultado = await servico.CriarAsync(NovaRequisicaoJfl(propriedadeId), CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(StatusEquipamento.Desconhecido, resultado.Data!.Status);
        Assert.Equal("SN-001", resultado.Data.NumeroSerie);
        equipamentos.Verify(e => e.AddAsync(It.IsAny<Equipamento>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_Jfl_IgnoraIpPortaUsuarioSenhaMesmoSeEnviados()
    {
        var (equipamentos, propriedades, modelos, _, _, criptografia, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        equipamentos.Setup(e => e.ExisteNumeroSerieDuplicadoAsync(propriedadeId, "SN-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        modelos.Setup(m => m.GetByFabricanteENomeAsync(It.IsAny<FabricanteEquipamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModeloEquipamento?)null);

        var request = NovaRequisicaoJfl(propriedadeId, ip: "10.0.0.1", porta: 4370, usuario: "root", senha: "root");

        var resultado = await servico.CriarAsync(request, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Null(resultado.Data!.Ip);
        Assert.Null(resultado.Data.Porta);
        Assert.Null(resultado.Data.Usuario);
        criptografia.Verify(c => c.Criptografar(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComModeloJaCatalogado_ReaproveitaEmVezDeCriarDuplicado()
    {
        var (equipamentos, propriedades, modelos, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var modeloExistente = new ModeloEquipamento { Id = Guid.NewGuid(), Fabricante = FabricanteEquipamento.Jfl, Nome = "Active 100", CreatedAtUtc = DateTime.UtcNow };
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        equipamentos.Setup(e => e.ExisteNumeroSerieDuplicadoAsync(propriedadeId, "SN-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        modelos.Setup(m => m.GetByFabricanteENomeAsync(FabricanteEquipamento.Jfl, "Active 100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(modeloExistente);

        var requestComModelo = NovaRequisicaoJfl(propriedadeId, modelo: "Active 100");

        await servico.CriarAsync(requestComModelo, CancellationToken.None);

        modelos.Verify(m => m.AddAsync(It.IsAny<ModeloEquipamento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ControlId_SemCamposDeConexao_Falha()
    {
        var (_, propriedades, _, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));

        var request = NovaRequisicaoControlId(propriedadeId, senha: null);

        var resultado = await servico.CriarAsync(request, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task CriarAsync_ControlId_Sucesso_ConectaEPersisteApenasOQueOProviderDevolveu()
    {
        var (equipamentos, propriedades, modelos, controlId, _, criptografia, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        modelos.Setup(m => m.GetByFabricanteENomeAsync(It.IsAny<FabricanteEquipamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModeloEquipamento?)null);
        controlId
            .Setup(c => c.ConsultarInformacoesAsync(It.IsAny<ConexaoEquipamento>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InformacoesEquipamento { Versao = "2.14", NumeroSerie = "CTID-123" });

        var resultado = await servico.CriarAsync(NovaRequisicaoControlId(propriedadeId), CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(StatusEquipamento.Online, resultado.Data!.Status);
        Assert.Equal("CTID-123", resultado.Data.NumeroSerie);
        Assert.Equal("2.14", resultado.Data.InformacoesDescobertas?["Versao"]);
        Assert.False(resultado.Data.InformacoesDescobertas!.ContainsKey("NomeDispositivo"));
        criptografia.Verify(c => c.Criptografar("admin123"), Times.Once);
        equipamentos.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CriarAsync_ControlId_FalhaDeConexao_MarcaOfflineMasNaoReprovaOCadastro()
    {
        var (_, propriedades, modelos, controlId, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        modelos.Setup(m => m.GetByFabricanteENomeAsync(It.IsAny<FabricanteEquipamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModeloEquipamento?)null);
        controlId
            .Setup(c => c.ConsultarInformacoesAsync(It.IsAny<ConexaoEquipamento>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var resultado = await servico.CriarAsync(NovaRequisicaoControlId(propriedadeId), CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(StatusEquipamento.Offline, resultado.Data!.Status);
    }

    [Fact]
    public async Task CriarAsync_Intelbras_SemCamposDeConexao_Falha()
    {
        var (_, propriedades, _, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));

        var request = NovaRequisicaoIntelbras(propriedadeId, ip: null);

        var resultado = await servico.CriarAsync(request, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task CriarAsync_Intelbras_Sucesso_TestaConexaoESoAtualizaStatus()
    {
        var (_, propriedades, modelos, _, intelbras, criptografia, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        modelos.Setup(m => m.GetByFabricanteENomeAsync(It.IsAny<FabricanteEquipamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModeloEquipamento?)null);
        intelbras
            .Setup(i => i.TestarConexaoAsync(It.IsAny<ConexaoIntelbras>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoTesteConexaoIntelbras { Sucesso = true });

        var resultado = await servico.CriarAsync(NovaRequisicaoIntelbras(propriedadeId), CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(StatusEquipamento.Online, resultado.Data!.Status);
        Assert.Null(resultado.Data.InformacoesDescobertas);
        Assert.Null(resultado.Data.Usuario);
        criptografia.Verify(c => c.Criptografar("1234"), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_FabricanteNaoSuportado_Falha()
    {
        var (_, propriedades, _, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));

        var resultado = await servico.CriarAsync(new CriarEquipamentoAdminRequest
        {
            PropriedadeId = propriedadeId,
            Nome = "Câmera Teste",
            Fabricante = FabricanteEquipamento.Hikvision,
            EstadoOperacional = EstadoOperacionalEquipamento.Ativo,
        }, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task AtualizarAsync_NumeroSerieDuplicadoExcluindoOProprio_Falha()
    {
        var (equipamentos, _, _, _, _, _, servico) = NovoServico();
        var id = Guid.NewGuid();
        var equipamento = new Equipamento
        {
            Id = id,
            PropriedadeId = Guid.NewGuid(),
            Nome = "Central",
            Fabricante = FabricanteEquipamento.Jfl,
            Status = StatusEquipamento.Online,
            CreatedAtUtc = DateTime.UtcNow,
        };
        equipamentos.Setup(e => e.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(equipamento);
        equipamentos.Setup(e => e.ExisteNumeroSerieDuplicadoAsync(equipamento.PropriedadeId, "SN-002", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await servico.AtualizarAsync(id, new AtualizarEquipamentoAdminRequest
        {
            Nome = "Central",
            Fabricante = FabricanteEquipamento.Jfl,
            NumeroSerie = "SN-002",
        }, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task AtualizarAsync_ControlId_SenhaEmBranco_MantemSenhaAtual()
    {
        var (equipamentos, _, modelos, _, _, criptografia, servico) = NovoServico();
        var id = Guid.NewGuid();
        var equipamento = new Equipamento
        {
            Id = id,
            PropriedadeId = Guid.NewGuid(),
            Nome = "Catraca",
            Fabricante = FabricanteEquipamento.ControlId,
            Status = StatusEquipamento.Online,
            Ip = "192.168.1.50",
            Porta = 80,
            Usuario = "admin",
            SenhaCriptografada = "enc:admin123",
            CreatedAtUtc = DateTime.UtcNow,
        };
        equipamentos.Setup(e => e.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(equipamento);
        modelos.Setup(m => m.GetByFabricanteENomeAsync(It.IsAny<FabricanteEquipamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModeloEquipamento?)null);

        var resultado = await servico.AtualizarAsync(id, new AtualizarEquipamentoAdminRequest
        {
            Nome = "Catraca Renomeada",
            Fabricante = FabricanteEquipamento.ControlId,
            Ip = "192.168.1.51",
            Porta = 80,
            Usuario = "admin",
            Senha = null,
        }, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal("enc:admin123", equipamento.SenhaCriptografada);
        criptografia.Verify(c => c.Criptografar(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_ControlId_SemSenhaEEquipamentoNuncaTeveSenha_Falha()
    {
        var (equipamentos, _, _, _, _, _, servico) = NovoServico();
        var id = Guid.NewGuid();
        var equipamento = new Equipamento
        {
            Id = id,
            PropriedadeId = Guid.NewGuid(),
            Nome = "Catraca",
            Fabricante = FabricanteEquipamento.ControlId,
            Status = StatusEquipamento.Desconhecido,
            CreatedAtUtc = DateTime.UtcNow,
        };
        equipamentos.Setup(e => e.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(equipamento);

        var resultado = await servico.AtualizarAsync(id, new AtualizarEquipamentoAdminRequest
        {
            Nome = "Catraca",
            Fabricante = FabricanteEquipamento.ControlId,
            Ip = "192.168.1.51",
            Porta = 80,
            Usuario = "admin",
            Senha = null,
        }, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task ExcluirAsync_MarcaSoftDeleteSemRemoverFisicamente()
    {
        var (equipamentos, _, _, _, _, _, servico) = NovoServico();
        var id = Guid.NewGuid();
        var equipamento = new Equipamento
        {
            Id = id,
            PropriedadeId = Guid.NewGuid(),
            Nome = "Central",
            Fabricante = FabricanteEquipamento.Jfl,
            Status = StatusEquipamento.Online,
            CreatedAtUtc = DateTime.UtcNow,
        };
        equipamentos.Setup(e => e.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(equipamento);

        var resultado = await servico.ExcluirAsync(id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.True(equipamento.Excluido);
        Assert.NotNull(equipamento.DataExclusaoUtc);
    }

    [Fact]
    public async Task ExcluirAsync_Inexistente_Falha()
    {
        var (equipamentos, _, _, _, _, _, servico) = NovoServico();
        equipamentos.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Equipamento?)null);

        var resultado = await servico.ExcluirAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task ListarAsync_PorPadrao_NaoIncluiRemovidos()
    {
        var (equipamentos, _, _, _, _, _, servico) = NovoServico();
        equipamentos
            .Setup(e => e.ListarGlobalAsync(1, 20, null, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Equipamento>(), 0));

        await servico.ListarAsync(1, 20, null, null, null, incluirRemovidos: false, CancellationToken.None);

        equipamentos.Verify(e => e.ListarGlobalAsync(1, 20, null, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_ComIncluirRemovidos_PropagaParaORepositorio()
    {
        var (equipamentos, _, _, _, _, _, servico) = NovoServico();
        var equipamentoExcluido = new Equipamento
        {
            Id = Guid.NewGuid(),
            PropriedadeId = Guid.NewGuid(),
            Nome = "Central Excluída",
            Fabricante = FabricanteEquipamento.Jfl,
            Status = StatusEquipamento.Offline,
            CreatedAtUtc = DateTime.UtcNow,
            Excluido = true,
            DataExclusaoUtc = DateTime.UtcNow,
        };
        equipamentos
            .Setup(e => e.ListarGlobalAsync(1, 20, null, null, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Equipamento> { equipamentoExcluido }, 1));

        var resultado = await servico.ListarAsync(1, 20, null, null, null, incluirRemovidos: true, CancellationToken.None);

        equipamentos.Verify(e => e.ListarGlobalAsync(1, 20, null, null, null, true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(resultado.Itens[0].Excluido);
        Assert.NotNull(resultado.Itens[0].DataExclusaoUtc);
    }
}
