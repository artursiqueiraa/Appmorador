using System.Collections.Concurrent;

// Simulador HTTP local do protocolo Control iD — criado na Sprint 11 (Migração da
// Integração Control iD) só para validar AppMorador.Infrastructure.ControlId.ControlIdProvider
// via requisições HTTP reais de verdade (localhost), já que nenhum equipamento físico
// está disponível neste ambiente (decisão confirmada na Fase 1). NUNCA rodar em
// produção — é um duplo de teste, não parte do domínio do AppMorador.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Porta fixa e previsível — sem HTTPS (o hardware real do Control iD também só fala HTTP).
app.Urls.Add("http://localhost:9500");

var sessoesAtivas = new ConcurrentHashSet();
var proximoId = 1;
var usuariosPorRegistro = new ConcurrentDictionary<string, int>();

app.MapPost("/login.fcgi", async (HttpRequest request) =>
{
    var corpo = await request.ReadFromJsonAsync<LoginRequest>();
    if (string.IsNullOrWhiteSpace(corpo?.Login) || string.IsNullOrWhiteSpace(corpo?.Password))
    {
        return Results.BadRequest();
    }

    var sessao = Guid.NewGuid().ToString("N");
    sessoesAtivas.Add(sessao);
    return Results.Ok(new { session = sessao });
});

app.MapGet("/system_information.fcgi", (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new { version = "4.5.0-simulado", device_id = "SIM-0001", name = "Simulador Control iD" });
});

app.MapPost("/create_objects.fcgi", async (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    using var leitor = new StreamReader(request.Body);
    var corpo = await leitor.ReadToEndAsync();
    var id = Interlocked.Increment(ref proximoId);
    var registro = System.Text.RegularExpressions.Regex.Match(corpo, "\"registration\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;
    if (corpo.Contains("\"users\"") && registro.Length > 0)
    {
        usuariosPorRegistro[registro] = id;
    }

    return Results.Ok(new { id, ids = new[] { id } });
});

// Sprint 26 — rosto da pessoa (corpo binário da imagem) e remoção.
app.MapPost("/user_set_image.fcgi", async (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    using var memoria = new MemoryStream();
    await request.Body.CopyToAsync(memoria);
    Console.WriteLine($"[SIMULADOR] user_set_image: user_id={request.Query["user_id"]} bytes={memoria.Length}");
    return memoria.Length > 0 ? Results.Ok(new { }) : Results.BadRequest();
});

app.MapPost("/user_destroy_image.fcgi", async (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    using var leitor = new StreamReader(request.Body);
    Console.WriteLine($"[SIMULADOR] user_destroy_image: {await leitor.ReadToEndAsync()}");
    return Results.Ok(new { });
});

app.MapPost("/load_objects.fcgi", async (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    using var leitor = new StreamReader(request.Body);
    var corpo = await leitor.ReadToEndAsync();

    // Sprint 26 — busca de pessoa por registration (o rosto é ligado ao id interno).
    if (corpo.Contains("\"users\""))
    {
        var registro = System.Text.RegularExpressions.Regex.Match(corpo, "\"registration\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;
        return usuariosPorRegistro.TryGetValue(registro, out var id)
            ? Results.Ok(new { users = new[] { new { id, registration = registro } } })
            : Results.Ok(new { users = Array.Empty<object>() });
    }

    var agora = DateTimeOffset.UtcNow;
    var eventos = new[]
    {
        new { id = 1, @event = 0, time = agora.AddMinutes(-30).ToUnixTimeSeconds(), user_id = 1 },
        new { id = 2, @event = 1, time = agora.AddMinutes(-10).ToUnixTimeSeconds(), user_id = 2 },
    };

    return Results.Ok(new { access_logs = eventos });
});

// Sprint 24.5 — abrir porta (execute_actions, ação "door"). Só confere sessão e formato; registra
// no console para o teste ver que o comando chegou.
app.MapPost("/execute_actions.fcgi", async (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    using var leitor = new StreamReader(request.Body);
    var corpo = await leitor.ReadToEndAsync();
    Console.WriteLine($"[SIMULADOR] execute_actions: {corpo}");
    return corpo.Contains("\"door\"") ? Results.Ok(new { }) : Results.BadRequest();
});

app.Run();

static bool SessaoValida(HttpRequest request, ConcurrentHashSet sessoes) =>
    request.Query.TryGetValue("session", out var sessao) && sessoes.Contains(sessao.ToString());

internal sealed class LoginRequest
{
    public string? Login { get; set; }

    public string? Password { get; set; }
}

internal sealed class ConcurrentHashSet
{
    private readonly ConcurrentDictionary<string, byte> _valores = new();

    public void Add(string valor) => _valores.TryAdd(valor, 0);

    public bool Contains(string valor) => _valores.ContainsKey(valor);
}
