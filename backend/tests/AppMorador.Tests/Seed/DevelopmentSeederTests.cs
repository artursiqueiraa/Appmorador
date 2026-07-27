using AppMorador.Application.Autenticacao;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Snapshots;
using AppMorador.Infrastructure.Persistence;
using AppMorador.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AppMorador.Tests.Seed;

/// <summary>
/// Sprint 20 — sem este teste, a correção do seed só seria descoberta rodando o
/// backend de verdade contra um banco vazio (o Morador de exemplo já existe na
/// maioria dos bancos de desenvolvimento locais, então o bloco de câmeras nunca
/// re-executa nessas máquinas). EF Core InMemory isola cada teste num banco novo.
/// </summary>
public class DevelopmentSeederTests
{
    private static AppDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static IPasswordHasher CriarPasswordHasher()
    {
        var mock = new Mock<IPasswordHasher>();
        mock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash-fake");
        return mock.Object;
    }

    private static ISnapshotStorage CriarStorage()
    {
        var mock = new Mock<ISnapshotStorage>();
        mock.Setup(s => s.SaveAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("snapshots/fake/exemplo.png");
        return mock.Object;
    }

    [Fact]
    public async Task SeedAsync_PrimeiraExecucao_CriaGravadorTresCamerasEUmVinculo()
    {
        await using var db = CriarContexto();

        await DevelopmentSeeder.SeedAsync(db, CriarPasswordHasher(), CriarStorage(), NullLogger.Instance);

        Assert.Equal(1, await db.Gravadores.CountAsync());
        Assert.Equal(3, await db.Cameras.CountAsync());
        Assert.Equal(1, await db.VinculosZonaCamera.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_CameraEntrada_OnlineComImagemEUltimoSucesso()
    {
        await using var db = CriarContexto();

        await DevelopmentSeeder.SeedAsync(db, CriarPasswordHasher(), CriarStorage(), NullLogger.Instance);

        var entrada = await db.Cameras.SingleAsync(c => c.Nome == "Entrada");
        Assert.Equal(StatusCamera.Online, entrada.Status);
        Assert.NotNull(entrada.UltimoSnapshotPath);
        Assert.NotNull(entrada.UltimoSucessoCapturaUtc);
        Assert.NotNull(entrada.UltimaTentativaCapturaUtc);
    }

    [Fact]
    public async Task SeedAsync_CameraSala_OfflineSemImagemMasVinculadaAZona()
    {
        await using var db = CriarContexto();

        await DevelopmentSeeder.SeedAsync(db, CriarPasswordHasher(), CriarStorage(), NullLogger.Instance);

        var sala = await db.Cameras.SingleAsync(c => c.Nome == "Sala");
        Assert.Equal(StatusCamera.Offline, sala.Status);
        Assert.Null(sala.UltimoSnapshotPath);

        var vinculo = await db.VinculosZonaCamera.SingleAsync();
        Assert.Equal(sala.Id, vinculo.CameraId);
    }

    [Fact]
    public async Task SeedAsync_SegundaExecucao_NaoDuplicaNadaIdempotente()
    {
        await using var db = CriarContexto();
        var passwordHasher = CriarPasswordHasher();
        var storage = CriarStorage();

        await DevelopmentSeeder.SeedAsync(db, passwordHasher, storage, NullLogger.Instance);
        await DevelopmentSeeder.SeedAsync(db, passwordHasher, storage, NullLogger.Instance);

        Assert.Equal(1, await db.Gravadores.CountAsync());
        Assert.Equal(3, await db.Cameras.CountAsync());
        Assert.Equal(1, await db.VinculosZonaCamera.CountAsync());
        Assert.Equal(1, await db.Usuarios.CountAsync(u => u.Email == DevelopmentSeeder.EmailMorador));
    }
}
