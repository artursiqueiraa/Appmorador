using System.Collections.Concurrent;

// Simulador HTTP local da central Intelbras — criado na Sprint 15 (Integração
// Intelbras: Prova Definitiva da Arquitetura, ADR 0018) só para validar
// AppMorador.Infrastructure.Intelbras.IntelbrasProvider via requisições HTTP reais
// (localhost), já que não há central Intelbras real disponível neste ambiente e não
// há documentação oficial pública/referência já investigada neste projeto para um
// protocolo TCP proprietário Intelbras (diferente do que havia para JFL) — decisão
// consciente de modelar a central como uma API HTTP local (ver ADR 0018). NUNCA
// rodar em produção — é um duplo de teste, não parte do domínio do AppMorador.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Urls.Add("http://localhost:9600");

const string SenhaValida = "1234";
var sessoesAtivas = new ConcurrentHashSet();
var particoes = new ConcurrentDictionary<int, bool>(new[]
{
    new KeyValuePair<int, bool>(1, false),
    new KeyValuePair<int, bool>(2, false),
});
var temProblemaAtivo = false;

app.MapPost("/login", async (HttpRequest request) =>
{
    var corpo = await request.ReadFromJsonAsync<LoginRequest>();
    if (corpo?.Senha != SenhaValida)
    {
        return Results.Unauthorized();
    }

    var sessao = Guid.NewGuid().ToString("N");
    sessoesAtivas.Add(sessao);
    return Results.Ok(new { sessao });
});

app.MapGet("/status", (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(MontarStatus());
});

app.MapPost("/armar", async (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    var corpo = await request.ReadFromJsonAsync<ComandoRequest>();
    particoes[corpo?.Particao ?? 1] = true;
    return Results.Ok(MontarStatus());
});

app.MapPost("/desarmar", async (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    var corpo = await request.ReadFromJsonAsync<ComandoRequest>();
    particoes[corpo?.Particao ?? 1] = false;
    return Results.Ok(MontarStatus());
});

app.MapGet("/eventos", (HttpRequest request) =>
{
    if (!SessaoValida(request, sessoesAtivas))
    {
        return Results.Unauthorized();
    }

    var agora = DateTimeOffset.UtcNow;
    var eventos = new[]
    {
        new { codigo = "1130", descricao = "Disparo de zona 01", ocorridoEmUnix = agora.AddMinutes(-15).ToUnixTimeSeconds() },
        new { codigo = "3401", descricao = "Restauração de zona 01", ocorridoEmUnix = agora.AddMinutes(-5).ToUnixTimeSeconds() },
    };

    return Results.Ok(new { eventos });
});

app.Run();

object MontarStatus() => new
{
    particoes = particoes.OrderBy(p => p.Key).Select(p => new { numero = p.Key, armada = p.Value }),
    temProblemaAtivo,
};

static bool SessaoValida(HttpRequest request, ConcurrentHashSet sessoes) =>
    request.Query.TryGetValue("sessao", out var sessao) && sessoes.Contains(sessao.ToString());

internal sealed class LoginRequest
{
    public string? Senha { get; set; }
}

internal sealed class ComandoRequest
{
    public int Particao { get; set; }
}

internal sealed class ConcurrentHashSet
{
    private readonly ConcurrentDictionary<string, byte> _valores = new();

    public void Add(string valor) => _valores.TryAdd(valor, 0);

    public bool Contains(string valor) => _valores.ContainsKey(valor);
}
