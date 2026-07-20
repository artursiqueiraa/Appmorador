using System.Text;
using System.Text.Json.Serialization;
using AppMorador.Api;
using AppMorador.Api.Hosting;
using AppMorador.Api.Middleware;
using AppMorador.Application.Autenticacao;
using AppMorador.Infrastructure.Identity;
using AppMorador.Infrastructure.Persistence;
using AppMorador.Infrastructure.Persistence.Seed;
using AppMorador.Infrastructure.Snapshots;
using AppMorador.Jfl;
using AppMorador.Jfl.Server.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection nao configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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
    });

builder.Services.AddAuthorization();

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
    builder.Services.AddSwaggerGen();
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

// Ordem importa: excecao/headers primeiro (envolvem tudo), depois HTTPS/CORS/rate
// limit, depois autenticacao/autorizacao, so entao os controllers.
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
            await DevelopmentSeeder.SeedAsync(db, passwordHasher, seedLogger);
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
app.MapControllers();

app.Logger.LogInformation("Sistema pronto.");
app.Run();
