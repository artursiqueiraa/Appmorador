using AppMorador.Application.Autenticacao;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Snapshots;
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

    public const string EmailMaster = "master@appmorador.local";
    public const string SenhaMaster = "Master@123";

    // Sprint 21 (ADR 0021) — mesma lista de PropriedadeServico.PermissoesPlanoBasico:
    // o backfill precisa conceder exatamente o que uma Propriedade nova ganharia hoje,
    // senao propriedades criadas ANTES desta Sprint ficam bloqueadas por padrao assim
    // que endpoints futuros passarem a checar Permissao Funcional.
    private static readonly PermissaoFuncionalidade[] PermissoesPlanoBasico =
    [
        PermissaoFuncionalidade.CadastrarMorador,
        PermissaoFuncionalidade.CadastrarFacial,
        PermissaoFuncionalidade.CadastrarTag,
        PermissaoFuncionalidade.AbrirPortao,
        PermissaoFuncionalidade.VerCameras,
        PermissaoFuncionalidade.CriarVisitante,
    ];

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, ISnapshotStorage snapshotStorage, ILogger logger, CancellationToken ct = default)
    {
        var criados = 0;

        criados += await GarantirContaSimplesAsync(db, passwordHasher, logger, "Administrador", EmailAdministrador, SenhaAdministrador, ct);
        criados += await GarantirContaSimplesAsync(db, passwordHasher, logger, "Carlos Henrique", EmailSupervisor, SenhaSupervisor, ct);
        criados += await GarantirContaSimplesAsync(db, passwordHasher, logger, "Juliana Souza", EmailOperador, SenhaOperador, ct);
        criados += await GarantirMoradorComPropriedadeAsync(db, passwordHasher, logger, ct);
        // Sprint 20 — passo proprio, idempotente por conta PROPRIA (nao pelo gate da
        // conta Morador acima): num banco onde a conta Morador ja existia ANTES desta
        // Sprint (todo ambiente de desenvolvimento ja em uso), o bloco acima nunca
        // roda de novo, e sem este passo separado a propriedade de exemplo nunca
        // ganharia cameras — a Sprint ficaria impossivel de validar sem editar o banco
        // manualmente. Verifica a existencia de cameras da PROPRIEDADE, nao da conta.
        criados += await GarantirCamerasDeExemploAsync(db, snapshotStorage, logger, ct);

        // Sprint 21 (ADR 0021) — mesmo raciocinio dos passos acima: gates proprios,
        // independentes das contas de conveniencia, para funcionar tanto em banco novo
        // quanto em banco ja em uso desde antes desta Sprint.
        criados += await GarantirMasterPadraoAsync(db, passwordHasher, logger, ct);
        criados += await GarantirVinculosUsuarioPropriedadeAsync(db, logger, ct);

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

    /// <summary>
    /// Sprint 20 — Visualizacao de Cameras: 1 gravador (fabricante/credenciais
    /// ficticias, sem DVR real alcancavel neste ambiente) + 3 cameras para a
    /// propriedade de exemplo do Morador. "Entrada" e "Fundos" ganham uma imagem de
    /// exemplo real (PNG gerado em memoria, ver PlaceholderImageGenerator) — sem
    /// isso a aba Cameras nunca teria nada para mostrar sem um gravador de verdade
    /// conectado. "Sala" fica deliberadamente sem imagem (Offline, "Sem imagem")
    /// para esse Empty State tambem ser demonstravel, e vinculada a zona "Sala"
    /// (VinculoZonaCamera) para o fluxo de snapshot-por-alarme (Sprint 1-2)
    /// continuar exercitavel. Idempotente pela PROPRIA existencia de Camera na
    /// propriedade — deliberadamente independente do gate de
    /// <see cref="GarantirMoradorComPropriedadeAsync"/>, para backfillar cameras num
    /// banco onde a conta Morador ja existia antes desta Sprint.
    /// </summary>
    private static async Task<int> GarantirCamerasDeExemploAsync(AppDbContext db, ISnapshotStorage snapshotStorage, ILogger logger, CancellationToken ct)
    {
        var propriedade = await db.Propriedades
            .Include(p => p.Proprietario)
            .FirstOrDefaultAsync(p => p.Proprietario != null && p.Proprietario.Email == EmailMorador, ct);

        if (propriedade is null)
        {
            return 0;
        }

        if (await db.Cameras.AnyAsync(c => c.PropriedadeId == propriedade.Id, ct))
        {
            return 0;
        }

        var zonaSala = await db.Zonas
            .FirstOrDefaultAsync(z => z.Central != null && z.Central.PropriedadeId == propriedade.Id && z.Nome == "Sala", ct);

        var agora = DateTime.UtcNow;

        var gravador = new Gravador
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedade.Id,
            Fabricante = FabricanteGravador.Dahua,
            Ip = "192.168.1.100",
            Porta = 80,
            NomeAcesso = "admin",
            Senha = "admin123",
        };

        var cameraEntrada = new Camera
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedade.Id,
            GravadorId = gravador.Id,
            Canal = 1,
            Nome = "Entrada",
            Status = StatusCamera.Online,
        };

        var cameraSala = new Camera
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedade.Id,
            GravadorId = gravador.Id,
            Canal = 2,
            Nome = "Sala",
            Status = StatusCamera.Offline,
            UltimaTentativaCapturaUtc = agora.AddHours(-2),
        };

        var cameraFundos = new Camera
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedade.Id,
            GravadorId = gravador.Id,
            Canal = 3,
            Nome = "Fundos",
            Status = StatusCamera.Online,
        };

        db.Gravadores.Add(gravador);
        db.Cameras.AddRange(cameraEntrada, cameraSala, cameraFundos);

        if (zonaSala is not null)
        {
            db.VinculosZonaCamera.Add(new VinculoZonaCamera { Id = Guid.NewGuid(), ZonaId = zonaSala.Id, CameraId = cameraSala.Id });
        }

        await db.SaveChangesAsync(ct);

        // Imagem de exemplo gravada em disco DEPOIS do SaveChanges principal — o
        // caminho relativo devolvido por SaveAsync so existe apos a captura, e um
        // segundo SaveChanges (abaixo) persiste esse caminho nas 2 cameras "Online".
        var imagemEntrada = PlaceholderImageGenerator.GerarPngSolido(320, 180, 90, 140, 200);
        var imagemFundos = PlaceholderImageGenerator.GerarPngSolido(320, 180, 60, 100, 70);

        var capturadoEmEntrada = agora.AddMinutes(-2);
        cameraEntrada.UltimoSnapshotPath = await snapshotStorage.SaveAsync(propriedade.Id, capturadoEmEntrada, imagemEntrada, ct);
        cameraEntrada.UltimoSucessoCapturaUtc = capturadoEmEntrada;
        cameraEntrada.UltimaTentativaCapturaUtc = capturadoEmEntrada;

        cameraFundos.UltimoSnapshotPath = await snapshotStorage.SaveAsync(propriedade.Id, agora, imagemFundos, ct);
        cameraFundos.UltimoSucessoCapturaUtc = agora;
        cameraFundos.UltimaTentativaCapturaUtc = agora;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed de desenvolvimento: cameras de exemplo criadas para a propriedade {PropriedadeId} (1 gravador, 3 cameras).",
            propriedade.Id);
        return 1;
    }

    /// <summary>
    /// Sprint 21 (ADR 0021) — conta Master padrao (papel global, nunca dono de
    /// Propriedade) para o ambiente de desenvolvimento nao nascer sem nenhum usuario
    /// capaz de acessar os endpoints [RequerMaster]/[RequerInterno]/impersonation.
    /// Idempotente por RoleGlobal (nao so por email): se algum Master ja existe
    /// (ex.: criado via api/usuarios-internos), este passo nunca recria outro.
    /// </summary>
    private static async Task<int> GarantirMasterPadraoAsync(AppDbContext db, IPasswordHasher passwordHasher, ILogger logger, CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(u => u.RoleGlobal == RoleSistema.Master, ct))
        {
            return 0;
        }

        db.Usuarios.Add(new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Master AppMorador",
            Email = EmailMaster,
            SenhaHash = passwordHasher.Hash(SenhaMaster),
            RoleGlobal = RoleSistema.Master,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seed de desenvolvimento: conta Master criada ({Email} / {Senha}).", EmailMaster, SenhaMaster);
        return 1;
    }

    /// <summary>
    /// Sprint 21 (ADR 0021) — backfill do vinculo UsuarioPropriedade (Administrador)
    /// para toda Propriedade que ja existia ANTES desta Sprint (o unico ponto que cria
    /// esse vinculo hoje e PropriedadeServico.CreateAsync, que so roda para
    /// propriedades NOVAS). Sem este passo, nenhuma Permissao Funcional/endpoint que
    /// dependa de IPermissaoService funcionaria para dados ja existentes. Idempotente
    /// por Propriedade (verifica a ausencia do vinculo, nao uma conta especifica).
    /// </summary>
    private static async Task<int> GarantirVinculosUsuarioPropriedadeAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var propriedadesSemVinculo = await db.Propriedades
            .Where(p => !db.UsuariosPropriedade.Any(v => v.PropriedadeId == p.Id))
            .ToListAsync(ct);

        if (propriedadesSemVinculo.Count == 0)
        {
            return 0;
        }

        var agora = DateTime.UtcNow;

        foreach (var propriedade in propriedadesSemVinculo)
        {
            var vinculo = new UsuarioPropriedade
            {
                Id = Guid.NewGuid(),
                UsuarioId = propriedade.ProprietarioId,
                PropriedadeId = propriedade.Id,
                Perfil = PerfilPropriedade.Administrador,
                CreatedAtUtc = agora,
            };
            db.UsuariosPropriedade.Add(vinculo);
            await db.SaveChangesAsync(ct);

            db.UsuariosPropriedadePermissao.AddRange(PermissoesPlanoBasico.Select(permissao => new UsuarioPropriedadePermissao
            {
                Id = Guid.NewGuid(),
                UsuarioPropriedadeId = vinculo.Id,
                Permissao = permissao,
            }));
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "Seed de desenvolvimento: backfill de UsuarioPropriedade criado para {Quantidade} propriedade(s) pre-existente(s).",
            propriedadesSemVinculo.Count);
        return propriedadesSemVinculo.Count;
    }
}
