using AppMorador.Application.Cameras;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using AppMorador.Domain.Snapshots;
using Moq;
using Xunit;

namespace AppMorador.Tests.Cameras;

public class CameraServicoTests
{
    private static Propriedade NovaPropriedade(Guid id, Guid proprietarioId) => new()
    {
        Id = id,
        Nome = "Casa Serra",
        Tipo = TipoPropriedade.Residencial,
        ProprietarioId = proprietarioId,
    };

    private static Camera NovaCamera(Guid propriedadeId, Propriedade propriedade, StatusCamera status = StatusCamera.Desconhecido, string? snapshotPath = null) => new()
    {
        Id = Guid.NewGuid(),
        PropriedadeId = propriedadeId,
        Propriedade = propriedade,
        GravadorId = Guid.NewGuid(),
        Canal = 1,
        Nome = "Entrada",
        Status = status,
        UltimoSnapshotPath = snapshotPath,
    };

    private static (Mock<IPropriedadeRepositorio> Propriedades, Mock<ICameraRepositorio> Cameras, Mock<ISnapshotStorage> Storage, Mock<ISnapshotCaptureService> Captura, Mock<IOperacionalEventoPublicador> Publicador, CameraServico Servico)
        NovoServico()
    {
        var propriedades = new Mock<IPropriedadeRepositorio>();
        var cameras = new Mock<ICameraRepositorio>();
        var storage = new Mock<ISnapshotStorage>();
        var captura = new Mock<ISnapshotCaptureService>();
        var publicador = new Mock<IOperacionalEventoPublicador>();
        var servico = new CameraServico(propriedades.Object, cameras.Object, storage.Object, captura.Object, publicador.Object);
        return (propriedades, cameras, storage, captura, publicador, servico);
    }

    [Fact]
    public async Task ListByPropriedadeAsync_PropriedadeDeOutroUsuario_Falha()
    {
        var (propriedades, _, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NovaPropriedade(propriedadeId, Guid.NewGuid()));

        var resultado = await servico.ListByPropriedadeAsync(Guid.NewGuid(), propriedadeId, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task ListByPropriedadeAsync_DonoCorreto_RetornaCamerasMapeadas()
    {
        var (propriedades, cameras, _, _, _, servico) = NovoServico();
        var proprietarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(propriedade);
        cameras.Setup(c => c.ListByPropriedadeAsync(propriedadeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([NovaCamera(propriedadeId, propriedade, StatusCamera.Online, "snapshots/x.jpg")]);

        var resultado = await servico.ListByPropriedadeAsync(proprietarioId, propriedadeId, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Single(resultado.Data!);
        Assert.Equal("Entrada", resultado.Data![0].Nome);
        Assert.NotNull(resultado.Data[0].UltimaImagemUrl);
    }

    [Fact]
    public async Task ObterSnapshotAsync_CameraSemImagem_RetornaOkNulo()
    {
        var (_, cameras, _, _, _, servico) = NovoServico();
        var proprietarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade);
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        var resultado = await servico.ObterSnapshotAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Null(resultado.Data);
    }

    [Fact]
    public async Task ObterSnapshotAsync_CameraDeOutroUsuario_Falha()
    {
        var (_, cameras, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, Guid.NewGuid());
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Online, "snapshots/x.jpg");
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        var resultado = await servico.ObterSnapshotAsync(Guid.NewGuid(), camera.Id, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task CapturarSnapshotAsync_Sucesso_AtualizaStatusParaOnlineEPublicaEvento()
    {
        var (_, cameras, _, captura, publicador, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Desconhecido);
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);
        captura.Setup(s => s.CapturarPorCameraIdAsync(camera.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SnapshotResult.Ok("snapshots/nova.jpg"));

        var resultado = await servico.CapturarSnapshotAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.True(resultado.Data!.Sucesso);
        Assert.Equal(StatusCamera.Online, camera.Status);
        Assert.Equal("snapshots/nova.jpg", camera.UltimoSnapshotPath);
        cameras.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        publicador.Verify(p => p.PublicarCameraStatusAsync(propriedadeId, It.IsAny<CameraStatusEvento>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CapturarSnapshotAsync_Falha_AtualizaStatusParaOfflineComMensagemAmigavel()
    {
        var (_, cameras, _, captura, publicador, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Online);
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);
        captura.Setup(s => s.CapturarPorCameraIdAsync(camera.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SnapshotResult.Fail("Snapshot nao obtido (timeout, autenticacao falhou, ou resposta HTTP nao-2xx)."));

        var resultado = await servico.CapturarSnapshotAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.False(resultado.Data!.Sucesso);
        Assert.Equal(StatusCamera.Offline, camera.Status);
        Assert.NotNull(resultado.Data.MensagemErro);
        Assert.DoesNotContain("HTTP", resultado.Data.MensagemErro);
        publicador.Verify(p => p.PublicarCameraStatusAsync(propriedadeId, It.IsAny<CameraStatusEvento>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CapturarSnapshotAsync_StatusNaoMuda_NaoPublicaEvento()
    {
        var (_, cameras, _, captura, publicador, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Online, "snapshots/antiga.jpg");
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);
        captura.Setup(s => s.CapturarPorCameraIdAsync(camera.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SnapshotResult.Ok("snapshots/nova.jpg"));

        await servico.CapturarSnapshotAsync(proprietarioId, camera.Id, CancellationToken.None);

        publicador.Verify(p => p.PublicarCameraStatusAsync(It.IsAny<Guid>(), It.IsAny<CameraStatusEvento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CapturarSnapshotAsync_ExcecaoDeCancelamentoPorTimeoutInterno_NuncaLancaERetornaFalhaAmigavel()
    {
        var (_, cameras, _, captura, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Online);
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        // Simula o timeout interno de 15s expirando antes da chamada ao provider retornar.
        captura.Setup(s => s.CapturarPorCameraIdAsync(camera.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var resultado = await servico.CapturarSnapshotAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.False(resultado.Data!.Sucesso);
        Assert.Equal(StatusCamera.Offline, camera.Status);
        Assert.Contains("demorou", resultado.Data.MensagemErro);
    }

    [Fact]
    public async Task ObterStatusAsync_Offline_TemMotivoAmigavel()
    {
        var (_, cameras, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Offline);
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        var resultado = await servico.ObterStatusAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.NotNull(resultado.Data!.MotivoIndisponibilidade);
    }

    [Fact]
    public async Task ObterStatusAsync_Online_SemMotivo()
    {
        var (_, cameras, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Online);
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        var resultado = await servico.ObterStatusAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Null(resultado.Data!.MotivoIndisponibilidade);
    }

    [Fact]
    public async Task ObterImagemAsync_SemSnapshot_Falha()
    {
        var (_, cameras, _, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade);
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        var resultado = await servico.ObterImagemAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task ObterImagemAsync_ArquivoJpeg_SniffaContentTypeCorreto()
    {
        var (_, cameras, storage, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Online, "snapshots/x.jpg");
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        var bytesJpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        storage.Setup(s => s.OpenRead("snapshots/x.jpg")).Returns(new MemoryStream(bytesJpeg));

        var resultado = await servico.ObterImagemAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal("image/jpeg", resultado.Data!.ContentType);
    }

    [Fact]
    public async Task ObterImagemAsync_ArquivoPng_SniffaContentTypeCorreto()
    {
        var (_, cameras, storage, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var proprietarioId = Guid.NewGuid();
        var propriedade = NovaPropriedade(propriedadeId, proprietarioId);
        var camera = NovaCamera(propriedadeId, propriedade, StatusCamera.Online, "snapshots/x.png");
        cameras.Setup(c => c.GetByIdAsync(camera.Id, It.IsAny<CancellationToken>())).ReturnsAsync(camera);

        var bytesPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        storage.Setup(s => s.OpenRead("snapshots/x.png")).Returns(new MemoryStream(bytesPng));

        var resultado = await servico.ObterImagemAsync(proprietarioId, camera.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal("image/png", resultado.Data!.ContentType);
    }
}
