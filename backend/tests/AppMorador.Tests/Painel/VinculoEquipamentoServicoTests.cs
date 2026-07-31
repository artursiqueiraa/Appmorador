using AppMorador.Application.Auditoria;
using AppMorador.Application.Painel.VinculosEquipamento;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Painel;

public class VinculoEquipamentoServicoTests
{
    private static (Mock<IVinculoEquipamentoPropriedadeRepositorio> Vinculos, Mock<IEquipamentoRepositorio> Equipamentos,
        Mock<IPropriedadeRepositorio> Propriedades, Mock<IUsuarioRepositorio> Usuarios, Mock<IAuditoriaService> Auditoria,
        VinculoEquipamentoServico Servico) NovoServico()
    {
        var vinculos = new Mock<IVinculoEquipamentoPropriedadeRepositorio>();
        var equipamentos = new Mock<IEquipamentoRepositorio>();
        var propriedades = new Mock<IPropriedadeRepositorio>();
        var usuarios = new Mock<IUsuarioRepositorio>();
        var auditoria = new Mock<IAuditoriaService>();
        var servico = new VinculoEquipamentoServico(vinculos.Object, equipamentos.Object, propriedades.Object, usuarios.Object, auditoria.Object);
        return (vinculos, equipamentos, propriedades, usuarios, auditoria, servico);
    }

    private static Equipamento NovoEquipamento(Guid id, Guid propriedadeId) => new()
    {
        Id = id,
        PropriedadeId = propriedadeId,
        Nome = "Central Teste",
        Fabricante = FabricanteEquipamento.Jfl,
        Status = StatusEquipamento.Online,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private static Propriedade NovaPropriedade(Guid id) =>
        new() { Id = id, Nome = "Casa Teste", Tipo = TipoPropriedade.Residencial, ProprietarioId = Guid.NewGuid() };

    private static Usuario NovoUsuario(Guid id) => new()
    {
        Id = id,
        Nome = "Técnico Teste",
        Email = $"{id}@teste.local",
        SenhaHash = "hash",
        CreatedAtUtc = DateTime.UtcNow,
    };

    [Fact]
    public async Task ProvisionarAsync_EquipamentoJaProvisionadoEmOutraPropriedade_Falha()
    {
        var (vinculos, equipamentos, propriedades, _, auditoria, servico) = NovoServico();
        var equipamentoId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        equipamentos.Setup(e => e.GetByIdAsync(equipamentoId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoEquipamento(equipamentoId, propriedadeId));
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(equipamentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VinculoEquipamentoPropriedade
            {
                Id = Guid.NewGuid(),
                EquipamentoId = equipamentoId,
                PropriedadeId = Guid.NewGuid(),
                DataInicioUtc = DateTime.UtcNow,
                CriadoPorUsuarioId = Guid.NewGuid(),
            });

        var resultado = await servico.ProvisionarAsync(
            Guid.NewGuid(), new ProvisionarEquipamentoRequest { EquipamentoId = equipamentoId, PropriedadeId = propriedadeId }, null, CancellationToken.None);

        Assert.False(resultado.Success);
        vinculos.Verify(v => v.AddAsync(It.IsAny<VinculoEquipamentoPropriedade>(), It.IsAny<CancellationToken>()), Times.Never);
        auditoria.Verify(a => a.RegistrarAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TipoAcaoAuditoria>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionarAsync_Sucesso_CriaVinculoEAudita()
    {
        var (vinculos, equipamentos, propriedades, usuarios, auditoria, servico) = NovoServico();
        var equipamentoId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        equipamentos.Setup(e => e.GetByIdAsync(equipamentoId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoEquipamento(equipamentoId, propriedadeId));
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(equipamentoId, It.IsAny<CancellationToken>())).ReturnsAsync((VinculoEquipamentoPropriedade?)null);
        usuarios.Setup(u => u.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoUsuario(usuarioId));

        var resultado = await servico.ProvisionarAsync(
            usuarioId, new ProvisionarEquipamentoRequest { EquipamentoId = equipamentoId, PropriedadeId = propriedadeId }, "127.0.0.1", CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.True(resultado.Data!.Ativo);
        Assert.Null(resultado.Data.DataFimUtc);
        vinculos.Verify(v => v.AddAsync(It.IsAny<VinculoEquipamentoPropriedade>(), It.IsAny<CancellationToken>()), Times.Once);
        auditoria.Verify(a => a.RegistrarAsync(
            usuarioId, "Técnico Teste", TipoAcaoAuditoria.Criar, "VinculoEquipamentoPropriedade", It.IsAny<string>(),
            It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrocarAsync_EquipamentoNovoJaProvisionado_Falha()
    {
        var (vinculos, equipamentos, propriedades, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var equipamentoAntigoId = Guid.NewGuid();
        var equipamentoNovoId = Guid.NewGuid();
        equipamentos.Setup(e => e.GetByIdAsync(equipamentoAntigoId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoEquipamento(equipamentoAntigoId, propriedadeId));
        equipamentos.Setup(e => e.GetByIdAsync(equipamentoNovoId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoEquipamento(equipamentoNovoId, propriedadeId));
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(equipamentoAntigoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VinculoEquipamentoPropriedade
            {
                Id = Guid.NewGuid(),
                EquipamentoId = equipamentoAntigoId,
                PropriedadeId = propriedadeId,
                DataInicioUtc = DateTime.UtcNow,
                CriadoPorUsuarioId = Guid.NewGuid(),
            });
        vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(equipamentoNovoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VinculoEquipamentoPropriedade
            {
                Id = Guid.NewGuid(),
                EquipamentoId = equipamentoNovoId,
                PropriedadeId = Guid.NewGuid(),
                DataInicioUtc = DateTime.UtcNow,
                CriadoPorUsuarioId = Guid.NewGuid(),
            });

        var resultado = await servico.TrocarAsync(Guid.NewGuid(), new TrocarEquipamentoRequest
        {
            PropriedadeId = propriedadeId,
            EquipamentoAntigoId = equipamentoAntigoId,
            EquipamentoNovoId = equipamentoNovoId,
        }, null, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task TrocarAsync_Sucesso_EncerraAntigoECriaNovoSemEditarOAntigo()
    {
        var (vinculos, equipamentos, propriedades, usuarios, auditoria, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var equipamentoAntigoId = Guid.NewGuid();
        var equipamentoNovoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var vinculoAntigo = new VinculoEquipamentoPropriedade
        {
            Id = Guid.NewGuid(),
            EquipamentoId = equipamentoAntigoId,
            PropriedadeId = propriedadeId,
            DataInicioUtc = DateTime.UtcNow.AddDays(-30),
            CriadoPorUsuarioId = Guid.NewGuid(),
        };
        equipamentos.Setup(e => e.GetByIdAsync(equipamentoAntigoId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoEquipamento(equipamentoAntigoId, propriedadeId));
        equipamentos.Setup(e => e.GetByIdAsync(equipamentoNovoId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoEquipamento(equipamentoNovoId, propriedadeId));
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId));
        vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(equipamentoAntigoId, It.IsAny<CancellationToken>())).ReturnsAsync(vinculoAntigo);
        vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(equipamentoNovoId, It.IsAny<CancellationToken>())).ReturnsAsync((VinculoEquipamentoPropriedade?)null);
        usuarios.Setup(u => u.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoUsuario(usuarioId));

        var resultado = await servico.TrocarAsync(usuarioId, new TrocarEquipamentoRequest
        {
            PropriedadeId = propriedadeId,
            EquipamentoAntigoId = equipamentoAntigoId,
            EquipamentoNovoId = equipamentoNovoId,
        }, null, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(equipamentoNovoId, resultado.Data!.EquipamentoId);
        Assert.NotNull(vinculoAntigo.DataFimUtc);
        vinculos.Verify(v => v.AddAsync(It.Is<VinculoEquipamentoPropriedade>(novo => novo.EquipamentoId == equipamentoNovoId), It.IsAny<CancellationToken>()), Times.Once);
        auditoria.Verify(a => a.RegistrarAsync(
            usuarioId, It.IsAny<string>(), TipoAcaoAuditoria.Editar, "VinculoEquipamentoPropriedade", It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DesvincularAsync_SemVinculoAtivo_Falha()
    {
        var (vinculos, _, _, _, _, servico) = NovoServico();
        vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((VinculoEquipamentoPropriedade?)null);

        var resultado = await servico.DesvincularAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task DesvincularAsync_Sucesso_EncerraVinculoEAudita()
    {
        var (vinculos, _, _, usuarios, auditoria, servico) = NovoServico();
        var equipamentoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var vinculoAtivo = new VinculoEquipamentoPropriedade
        {
            Id = Guid.NewGuid(),
            EquipamentoId = equipamentoId,
            PropriedadeId = Guid.NewGuid(),
            DataInicioUtc = DateTime.UtcNow,
            CriadoPorUsuarioId = Guid.NewGuid(),
        };
        vinculos.Setup(v => v.GetVinculoAtivoPorEquipamentoAsync(equipamentoId, It.IsAny<CancellationToken>())).ReturnsAsync(vinculoAtivo);
        usuarios.Setup(u => u.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoUsuario(usuarioId));

        var resultado = await servico.DesvincularAsync(usuarioId, equipamentoId, "127.0.0.1", CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.NotNull(vinculoAtivo.DataFimUtc);
        auditoria.Verify(a => a.RegistrarAsync(
            usuarioId, It.IsAny<string>(), TipoAcaoAuditoria.Excluir, "VinculoEquipamentoPropriedade", It.IsAny<string>(),
            It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterDashboardAsync_CalculaDisponiveisComoDiferenca()
    {
        var (vinculos, equipamentos, _, _, _, servico) = NovoServico();
        equipamentos.Setup(e => e.ContarPorStatusGlobalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StatusEquipamento, int> { [StatusEquipamento.Online] = 6, [StatusEquipamento.Offline] = 4 });
        vinculos.Setup(v => v.ContarEquipamentosProvisionadosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var resultado = await servico.ObterDashboardAsync(CancellationToken.None);

        Assert.Equal(10, resultado.TotalEquipamentos);
        Assert.Equal(3, resultado.TotalProvisionados);
        Assert.Equal(7, resultado.TotalDisponiveis);
    }
}
