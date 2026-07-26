using AppMorador.Application.Autenticacao;
using AppMorador.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Persistence.Seed;

/// <summary>
/// Popula um banco de desenvolvimento vazio com contas e dados minimos para testar o
/// fluxo completo (login, propriedade, dashboard, central de eventos) sem depender de
/// um painel JFL real conectado. Idempotente por conta: cada usuario e verificado por
/// email antes de ser inserido, entao rodar de novo (ex.: a cada `dotnet run` em
/// Development) nunca duplica dados.
///
/// As 4 contas (Administrador/Supervisor/Operador/Morador) sao nomes de conveniencia
/// para dar variedade realista aos dados de teste — o dominio hoje NAO tem sistema de
/// Papel/Perfil (ver docs/DIVIDA_TECNICA.md item 6), entao as 4 sao contas comuns,
/// funcionalmente identicas. So a conta "Morador" (Fernanda Oliveira) tem uma
/// propriedade de exemplo vinculada, por ser a unica persona com sentido de dona de
/// propriedade no modelo atual (B2C self-service).
/// </summary>
public static class DevelopmentSeeder
{
    public const string EmailAdministrador = "admin@appmorador.local";
    public const string SenhaAdministrador = "Admin@123";

    public const string EmailSupervisor = "carlos.henrique@appmorador.local";
    public const string SenhaSupervisor = "Supervisor@123";

    public const string EmailOperador = "juliana.souza@appmorador.local";
    public const string SenhaOperador = "Operador@123";

    public const string EmailMorador = "fernanda.oliveira@appmorador.local";
    public const string SenhaMorador = "Morador@123";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, ILogger logger, CancellationToken ct = default)
    {
        var criados = 0;

        criados += await GarantirContaSimplesAsync(db, passwordHasher, logger, "Administrador", EmailAdministrador, SenhaAdministrador, ct);
        criados += await GarantirContaSimplesAsync(db, passwordHasher, logger, "Carlos Henrique", EmailSupervisor, SenhaSupervisor, ct);
        criados += await GarantirContaSimplesAsync(db, passwordHasher, logger, "Juliana Souza", EmailOperador, SenhaOperador, ct);
        criados += await GarantirMoradorComPropriedadeAsync(db, passwordHasher, logger, ct);

        if (criados == 0)
        {
            logger.LogInformation("Seed de desenvolvimento: todas as contas ja existem — nada a fazer.");
        }
    }

    private static async Task<int> GarantirContaSimplesAsync(
        AppDbContext db, IPasswordHasher passwordHasher, ILogger logger, string nome, string email, string senha, CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
        {
            return 0;
        }

        db.Usuarios.Add(new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Email = email,
            SenhaHash = passwordHasher.Hash(senha),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seed de desenvolvimento: conta {Nome} criada ({Email} / {Senha}).", nome, email, senha);
        return 1;
    }

    private static async Task<int> GarantirMoradorComPropriedadeAsync(
        AppDbContext db, IPasswordHasher passwordHasher, ILogger logger, CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(u => u.Email == EmailMorador, ct))
        {
            return 0;
        }

        var agora = DateTime.UtcNow;

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Fernanda Oliveira",
            Email = EmailMorador,
            SenhaHash = passwordHasher.Hash(SenhaMorador),
            CreatedAtUtc = agora,
        };

        var propriedade = new Propriedade
        {
            Id = Guid.NewGuid(),
            Nome = "Residencial Jardim das Flores",
            Tipo = TipoPropriedade.Residencial,
            Endereco = "Rua das Flores, 123 - Jardim das Flores",
            ProprietarioId = usuario.Id,
        };

        var central = new Central
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedade.Id,
            // "000001" e usado por dados de teste de sessoes anteriores que podem
            // ainda existir no banco de desenvolvimento (Central.NumeroSerie e unico)
            // — "000002" evita colisao sem depender de limpar dado antigo.
            NumeroSerie = "000002",
            Nome = "Central Modelo",
        };

        var zonaSala = new Zona { Id = Guid.NewGuid(), CentralId = central.Id, Numero = "001", Nome = "Sala" };
        var zonaGaragem = new Zona { Id = Guid.NewGuid(), CentralId = central.Id, Numero = "002", Nome = "Garagem" };

        // Ocorrencias com CreatedAtUtc espacado (6h entre cada) para exercitar de
        // verdade os filtros de periodo (Hoje/7 dias/30 dias/Tudo) da Central de
        // Eventos, nao so a listagem simples.
        var ocorrencias = Enumerable.Range(0, 5)
            .Select(i =>
            {
                var zona = i % 2 == 0 ? zonaSala : zonaGaragem;
                return new Ocorrencia
                {
                    Id = Guid.NewGuid(),
                    NumeroSeriePainel = central.NumeroSerie,
                    CodigoEvento = "1130",
                    ZonaOuUsuario = zona.Numero,
                    Particao = "1",
                    CreatedAtUtc = agora.AddHours(-6 * (i + 1)),
                    CentralId = central.Id,
                    PropriedadeId = propriedade.Id,
                    ZonaId = zona.Id,
                    StatusResolucao = StatusResolucao.Resolvido,
                };
            })
            .ToList();

        // Sprint 6 — dominio principal (Propriedade > Unidade > Morador): dado real
        // minimo para o Dashboard nao nascer com "0 unidades, 0 moradores" e o fluxo
        // criar-propriedade -> criar-unidade -> criar-morador ficar demonstravel sem
        // cadastro manual antes de qualquer teste.
        var unidade = new Unidade
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedade.Id,
            Tipo = TipoUnidade.Casa,
            Identificacao = "Casa principal",
            CreatedAtUtc = agora,
        };

        var moradores = new List<Morador>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UnidadeId = unidade.Id,
                Nome = "Fernanda Oliveira",
                Telefone = "27999990001",
                Email = EmailMorador,
                Status = StatusMorador.Ativo,
                CreatedAtUtc = agora,
            },
            new()
            {
                Id = Guid.NewGuid(),
                UnidadeId = unidade.Id,
                Nome = "Rafael Oliveira",
                Telefone = "27999990002",
                Status = StatusMorador.Ativo,
                Observacoes = "Cônjuge",
                CreatedAtUtc = agora,
            },
        };

        db.Usuarios.Add(usuario);
        db.Propriedades.Add(propriedade);
        db.Centrais.Add(central);
        db.Zonas.AddRange(zonaSala, zonaGaragem);
        db.Ocorrencias.AddRange(ocorrencias);
        db.Unidades.Add(unidade);
        db.Moradores.AddRange(moradores);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed de desenvolvimento: conta Morador criada ({Email} / {Senha}) com 1 propriedade, 1 central, 2 zonas, {QtdOcorrencias} ocorrencias, 1 unidade, {QtdMoradores} moradores.",
            EmailMorador, SenhaMorador, ocorrencias.Count, moradores.Count);
        return 1;
    }
}
