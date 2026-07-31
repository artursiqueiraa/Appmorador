using System.Text;
using System.Text.Json.Serialization;
using AppMorador.Api;
using AppMorador.Api.Auth;
using AppMorador.Api.Hosting;
using AppMorador.Api.Middleware;
using AppMorador.Api.Realtime;
using AppMorador.Application.Autenticacao;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Snapshots;
using AppMorador.Infrastructure.Identity;
using AppMorador.Infrastructure.Notifications;
using AppMorador.Infrastructure.Persistence;
using AppMorador.Infrastructure.Persistence.Seed;
using AppMorador.Infrastructure.Snapshots;
using AppMorador.Jfl;
using AppMorador.Jfl.Server.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Sprint 22B (ADR 0031) — habilita a impressão de BeginScope (CorrelationId, UsuarioId) nos
// logs de console; sem isso os escopos abertos pelos middlewares abaixo ficam inertes.
builder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection nao configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Sprint 11 — protege a senha de Equipamento em repouso (ver ADR 0014). Nome de
// aplicacao fixo garante que o keyring de cifragem seja o mesmo entre reinicios do
// processo (sem isso, cada instancia geraria chaves proprias e nada decifraria).
builder.Services.AddDataProtection().SetApplicationName("AppMorador");

// --- Auth / Propriedade / Dashboard (Sprint 1) -----------------------------------
// Jwt:Key NUNCA no appsettings.json committado: user-secrets em dev
// (dotnet user-secrets set "Jwt:Key" "...") ou variavel de ambiente Jwt__Key em producao.
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AppMorador";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AppMoradorApp";

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key nao configurada. Configure via user-secrets em dev " +
        "(dotnet user-secrets set \"Jwt:Key\" \"<chave-aleatoria-longa>\") ou variavel de ambiente Jwt__Key em producao.");
}

builder.Services.AddAuthModule(options =>
{
    options.Key = jwtKey;
    options.Issuer = jwtIssuer;
    options.Audience = jwtAudience;
    options.AccessTokenMinutes = builder.Configuration.GetValue("Jwt:AccessTokenMinutes", 20);
    options.RefreshTokenDays = builder.Configuration.GetValue("Jwt:RefreshTokenDays", 30);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sprint 21 (ADR 0021) bugfix — sem isto, o ASP.NET Core remapeia a claim
        // "role" (nome curto emitido por JwtTokenService) para a URI longa
        // ClaimTypes.Role por padrao (MapInboundClaims=true e o default), e
        // ClaimsPrincipalExtensions.GetRoleGlobal() (que procura literalmente
        // "role") nunca encontra nada — toda Policy RequerMaster/Tecnico/Suporte/
        // Interno ficava bloqueando ATE o Master de verdade (403 sempre), sem
        // nenhum teste pegar isso (os testes de ClaimsPrincipalExtensions
        // construiam o ClaimsPrincipal na mao, sem passar pelo pipeline real do
        // JwtBearerHandler). Descoberto na Fase 0 da Sprint 22A testando
        // impersonation contra o backend real.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // Sprint 14 (ADR 0017) — o handshake de transporte do SignalR (WebSocket) nao
        // permite cabecalhos customizados como Authorization; o cliente envia o token
        // via querystring so para as rotas do Hub. Nenhuma outra rota aceita token por
        // querystring (Controllers continuam exigindo o header Bearer normal).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

// Sprint 21 (ADR 0021) — todas as Policies do sistema, registradas uma única vez
// aqui. Nunca checar role/claim manualmente dentro de um Controller — sempre via
// [Authorize(Policy = ...)]. RequireAssertion (em vez de Requirement/Handler
// próprios por Policy) é uma simplificação deliberada: são checagens de claim
// simples, e um punhado de classes extras por Policy não traria isolamento real
// nenhum a mais do que já existe aqui, centralizado num único lugar.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RequerMaster, p => p.RequireAssertion(ctx => ctx.User.TemAlgumRoleGlobal(RoleSistema.Master)));
    options.AddPolicy(Policies.RequerTecnico, p => p.RequireAssertion(ctx => ctx.User.TemAlgumRoleGlobal(RoleSistema.Master, RoleSistema.Tecnico)));
    options.AddPolicy(Policies.RequerSuporte, p => p.RequireAssertion(ctx => ctx.User.TemAlgumRoleGlobal(RoleSistema.Master, RoleSistema.Suporte)));
    options.AddPolicy(Policies.RequerInterno, p => p.RequireAssertion(ctx => ctx.User.EhInterno()));

    // Sprint 21 — hoje "cliente" == "não é interno" (só Administrador tem login,
    // ver ADR 0021); RequerAdministrador é o mesmo predicado por ora, mantido como
    // nome próprio para o dia em que Morador também autenticar.
    options.AddPolicy(Policies.RequerCliente, p => p.RequireAssertion(ctx => !ctx.User.EhInterno()));
    options.AddPolicy(Policies.RequerAdministrador, p => p.RequireAssertion(ctx => !ctx.User.EhInterno()));

    // Reservado — nenhum login produz essa claim ainda (ver Policies.RequerMorador).
    options.AddPolicy(Policies.RequerMorador, p => p.RequireAssertion(ctx => ctx.User.HasClaim("perfilPropriedade", "Morador")));
});

// Sprint 21 (ADR 0021, Fase 4.2) — auditoria de falha de autorização centralizada,
// nunca espalhada em cada endpoint.
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuditoriaAuthorizationMiddlewareResultHandler>();

// Sprint 14 (ADR 0017) — camada de transporte da Camada Operacional (ADR 0016) em
// tempo real. IOperacionalEventoPublicador e Singleton porque so encapsula o
// IHubContext (ja Singleton) mais um dicionario de debounce compartilhado — nao
// carrega estado por requisicao.
// Enums de negocio devem serializar como texto tambem no SignalR (ADR 0005) — o
// protocolo JSON do Hub tem sua propria configuracao de serializacao, independente
// de AddControllers().AddJsonOptions() (que so afeta Controllers).
builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IOperacionalEventoPublicador, OperacionalHubPublicador>();

// Sprint 19 (ADR 0023) — notificacoes push (complemento ao SignalR acima: app
// aberto usa SignalR, app fechado usa push). Sem Firebase:CredenciaisPath
// configurado, opera em modo documentado "sem Firebase" (ver FirebaseOptions).
builder.Services.AddNotificationsModule(builder.Configuration);

// Rate limit no login/registro — mitiga brute force e cadastro em massa.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimiterPolicies.Auth, limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueLimit = 0;
    });

    // Sprint 14 — protege o endpoint de negotiate/conexao do OperacionalHub contra
    // abertura em massa de conexoes; nao limita mensagens dentro de uma conexao ja
    // aberta (SignalR nao expoe isso via este mecanismo).
    options.AddFixedWindowLimiter(RateLimiterPolicies.Realtime, limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 30;
        limiterOptions.QueueLimit = 0;
    });
});

// CORS restritivo: sem origem configurada, nenhuma origem cruzada e liberada
// (nunca AllowAnyOrigin). Preencher Cors:AllowedOrigins quando o painel web existir.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// Enums de negocio (ex.: TipoPropriedade) trafegam como texto legivel na Api — nunca
// como numero interno — tanto na leitura quanto na escrita. Configuracao global para
// nao precisar de um atributo por DTO a cada enum novo exposto.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger so em Development — nunca exposto em producao.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Sprint 22B (ADR 0031) — inclui os comentários /// dos Controllers/DTOs no Swagger UI
        // (descrições de endpoint, request/response). Arquivo sempre existe (GenerateDocumentationFile
        // habilitado no .csproj), então nenhuma checagem de existência é necessária aqui.
        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
        options.IncludeXmlComments(xmlPath);
    });
}

// --- Alarme JFL + Snapshot (fases anteriores, sem alteracao de comportamento) ----
// Servidor TCP JFL (recebe conexoes iniciadas pelas centrais — o painel disca para
// fora, nunca o contrario). Infraestrutura de protocolo (conexao, keep-alive) vem de
// AppMorador.Jfl. EventoCommandHandler e so um adaptador fino de protocolo (parse +
// ACK); todo o processamento de negocio (filtro, log de auditoria, resolucao de
// painel/zona, criacao de Ocorrencia) vive em AlarmEventProcessor (Scoped).
builder.Services.AddJflServer(options =>
{
    options.Porta = builder.Configuration.GetValue("Jfl:Porta", 8085);
    options.IntervaloKeepAliveMinutos = (byte)builder.Configuration.GetValue("Jfl:IntervaloKeepAliveMinutos", 5);
});
builder.Services.AddScoped<AppMorador.Infrastructure.Jfl.AlarmEventProcessor>();
builder.Services.AddSingleton<IJflCommandHandler, AppMorador.Infrastructure.Jfl.EventoCommandHandler>();
builder.Services.AddHostedService<JflServerHostedService>();

// Sprint 22C.2 — hook de conexão JFL: marca o Equipamento correspondente como Online +
// descoberta automática quando a central termina o handshake (SessaoRegistrada). Puramente
// aditivo sobre AppMorador.Jfl (nenhuma mudança no protocolo em si).
builder.Services.AddHostedService<AppMorador.Infrastructure.Jfl.EquipamentoJflConexaoObserver>();

// Captura de snapshot (disco local, sem nuvem) — chamada pelo AlarmEventProcessor
// depois que a Ocorrencia ja foi criada.
builder.Services.AddSnapshotCapture(options =>
{
    options.BasePath = builder.Configuration.GetValue("Snapshots:BasePath", "snapshots")!;
    options.TimeoutSeconds = builder.Configuration.GetValue("Snapshots:TimeoutSeconds", 5);
});

var app = builder.Build();

// Migrations aplicadas automaticamente no startup, em qualquer ambiente (nao so
// Development): MigrateAsync cria o banco se ele nao existir e aplica so as
// migrations pendentes contra __EFMigrationsHistory — nunca recria nem apaga um
// banco/dado existente. E uma dependencia dura (mesmo criterio que Jwt:Key/connection
// string): falha aqui deve interromper a inicializacao, nunca ser silenciada como o
// JFL Server. Ver ADR 0008.
using (var migrationScope = app.Services.CreateScope())
{
    var db = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var migrationLogger = migrationScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    migrationLogger.LogInformation("Verificando banco de dados...");
    var pendentes = (await db.Database.GetPendingMigrationsAsync()).ToList();

    if (pendentes.Count > 0)
    {
        migrationLogger.LogInformation(
            "Migration(ns) pendente(s) encontrada(s): {Migrations}. Aplicando...",
            string.Join(", ", pendentes));
        await db.Database.MigrateAsync();
        migrationLogger.LogInformation("Migration(ns) aplicada(s) com sucesso.");
    }
    else
    {
        migrationLogger.LogInformation("Banco localizado, nenhuma migration pendente.");
    }
}

// Ordem importa: CorrelationId primeiro (envolve tudo, inclusive erros antes da
// autenticacao), depois excecao/headers, depois HTTPS/CORS/rate limit, depois
// autenticacao/autorizacao (so entao UsuarioLogadoEnrichmentMiddleware, que precisa
// de HttpContext.User ja populado), so entao os controllers.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seed de desenvolvimento: idempotente (verifica antes de inserir), so roda em
    // Development, e uma falha aqui nunca derruba a Api — mesma filosofia do JFL
    // Server, um servico secundario nao pode impedir a Api de subir.
    using (var seedScope = app.Services.CreateScope())
    {
        var seedLogger = seedScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            seedLogger.LogInformation("Executando seed de desenvolvimento...");
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var snapshotStorage = seedScope.ServiceProvider.GetRequiredService<ISnapshotStorage>();
            await DevelopmentSeeder.SeedAsync(db, passwordHasher, snapshotStorage, seedLogger);
        }
        catch (Exception ex)
        {
            seedLogger.LogError(ex, "Seed de desenvolvimento falhou — a Api continua subindo normalmente, mas os dados de teste podem nao estar disponiveis.");
        }
    }
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UsuarioLogadoEnrichmentMiddleware>();
app.MapControllers();

// Sprint 14 (ADR 0017) — transporte em tempo real da Camada Operacional (ADR 0016).
app.MapHub<OperacionalHub>("/hubs/operacional").RequireRateLimiting(RateLimiterPolicies.Realtime);

app.Logger.LogInformation("Sistema pronto.");
app.Run();
