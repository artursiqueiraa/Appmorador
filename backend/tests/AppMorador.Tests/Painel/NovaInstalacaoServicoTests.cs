using AppMorador.Application.Auditoria;
using AppMorador.Application.Autenticacao;
using AppMorador.Application.Common;
using AppMorador.Application.Painel.NovasInstalacoes;
using AppMorador.Application.Painel.VinculosEquipamento;
using AppMorador.Application.Propriedades;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Painel;

public class NovaInstalacaoServicoTests
{
    private sealed record Dependencias(
        Mock<IUsuarioRepositorio> Usuarios,
        Mock<IEquipamentoRepositorio> Equipamentos,
        Mock<IVinculoEquipamentoPropriedadeRepositorio> Vinculos,
        Mock<IAutenticacaoServico> Autenticacao,
        Mock<IPropriedadeServico> Propriedades,
        Mock<IVinculoEquipamentoServico> Provisionamento,
        Mock<IAuditoriaService> Auditoria,
        NovaInstalacaoServico Servico);

    private static readonly Guid MasterId = Guid.NewGuid();
    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid PropriedadeId = Guid.NewGuid();
    private static readonly Guid EquipamentoId = Guid.NewGuid();

    private static Dependencias NovoServico()
    {
        var d = new Dependencias(
            new Mock<IUsuarioRepositorio>(),
            new Mock<IEquipamentoRepositorio>(),
            new Mock<IVinculoEquipamentoPropriedadeRepositorio>(),
            new Mock<IAutenticacaoServico>(),
            new Mock<IPropriedadeServico>(),
            new Mock<IVinculoEquipamentoServico>(),
            new Mock<IAuditoriaService>(),
            null!);

        d.Autenticacao.Setup(a => a.RegisterAsync(It.IsAny<CadastrarUsuarioRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Ok(ClienteId));
        d.Propriedades.Setup(p => p.CreateAsync(ClienteId, It.IsAny<CriarPropriedadeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropriedadeResponse { Id = PropriedadeId, Nome = "Casa Centro", Tipo = TipoPropriedade.Residencial });
        d.Provisionamento.Setup(p => p.ProvisionarAsync(MasterId, It.IsAny<ProvisionarEquipamentoRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VinculoResponse>.Ok(new VinculoResponse
            {
                Id = Guid.NewGuid(),
                EquipamentoId = EquipamentoId,
                PropriedadeId = PropriedadeId,
                DataInicioUtc = DateTime.UtcNow,
                Ativo = true,
                CriadoPorUsuarioId = MasterId,
            }));

        var servico = new NovaInstalacaoServico(
            d.Usuarios.Object, d.Equipamentos.Object, d.Vinculos.Object, d.Autenticacao.Object,
            d.Propriedades.Object, d.Provisionamento.Object, d.Auditoria.Object);
        return d with { Servico = servico };
    }

    private static Equipamento NovoEquipamento() => new()
    {
        Id = EquipamentoId,
        Nome = "Central",
        Fabricante = FabricanteEquipamento.Jfl,
        Status = StatusEquipamento.Offline,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private static CriarNovaInstalacaoRequest Pedido(Guid? equipamentoId = null) => new()
    {
        NomeCliente = "Ana Souza",
        EmailCliente = " Ana@Exemplo.com ",
        SenhaInicial = "Senha123",
        NomePropriedade = "Casa Centro",
        TipoPropriedade = TipoPropriedade.Residencial,
        EquipamentoId = equipamentoId,
    };

    [Fact]
    public async Task CriarAsync_EmailJaUsado_FalhaSemCriarNada()
    {
        var d = NovoServico();
        d.Usuarios.Setup(u => u.GetByEmailAsync("ana@exemplo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = Guid.NewGuid(), Nome = "Outra", Email = "ana@exemplo.com", SenhaHash = "x", CreatedAtUtc = DateTime.UtcNow });

        var resultado = await d.Servico.CriarAsync(MasterId, Pedido(), null, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal("Já existe uma conta com este e-mail.", resultado.Error);
        d.Autenticacao.Verify(a => a.RegisterAsync(It.IsAny<CadastrarUsuarioRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_EquipamentoJaProvisionado_FalhaAntesDeCriarCliente()
    {
        var d = NovoServico();
        d.Equipamentos.Setup(e => e.GetByIdAsync(EquipamentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NovoEquipamento());
        d.Vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(EquipamentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VinculoEquipamentoPropriedade { Id = Guid.NewGuid(), EquipamentoId = EquipamentoId, PropriedadeId = Guid.NewGuid(), DataInicioUtc = DateTime.UtcNow });

        var resultado = await d.Servico.CriarAsync(MasterId, Pedido(EquipamentoId), null, CancellationToken.None);

        Assert.False(resultado.Success);
        d.Autenticacao.Verify(a => a.RegisterAsync(It.IsAny<CadastrarUsuarioRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        d.Propriedades.Verify(p => p.CreateAsync(It.IsAny<Guid>(), It.IsAny<CriarPropriedadeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_EquipamentoInexistente_Falha()
    {
        var d = NovoServico();

        var resultado = await d.Servico.CriarAsync(MasterId, Pedido(EquipamentoId), null, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal("Equipamento não encontrado.", resultado.Error);
    }

    [Fact]
    public async Task CriarAsync_ComEquipamento_CriaClientePropriedadeEVinculo()
    {
        var d = NovoServico();
        d.Equipamentos.Setup(e => e.GetByIdAsync(EquipamentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NovoEquipamento());

        var resultado = await d.Servico.CriarAsync(MasterId, Pedido(EquipamentoId), "10.0.0.1", CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(ClienteId, resultado.Data!.ClienteId);
        Assert.Equal(PropriedadeId, resultado.Data.PropriedadeId);
        Assert.NotNull(resultado.Data.VinculoEquipamentoId);
        d.Autenticacao.Verify(a => a.RegisterAsync(
            It.Is<CadastrarUsuarioRequest>(r => r.Email == "ana@exemplo.com" && r.Senha == "Senha123"), It.IsAny<CancellationToken>()), Times.Once);
        d.Provisionamento.Verify(p => p.ProvisionarAsync(
            MasterId, It.Is<ProvisionarEquipamentoRequest>(r => r.EquipamentoId == EquipamentoId && r.PropriedadeId == PropriedadeId),
            "10.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
        d.Auditoria.Verify(a => a.RegistrarAsync(
            MasterId, It.IsAny<string>(), TipoAcaoAuditoria.Criar, "Usuario", ClienteId.ToString(),
            It.IsAny<string?>(), "10.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_SemEquipamento_NaoProvisiona()
    {
        var d = NovoServico();

        var resultado = await d.Servico.CriarAsync(MasterId, Pedido(), null, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Null(resultado.Data!.VinculoEquipamentoId);
        d.Provisionamento.Verify(p => p.ProvisionarAsync(
            It.IsAny<Guid>(), It.IsAny<ProvisionarEquipamentoRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
